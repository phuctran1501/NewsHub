namespace NewsHub.Application.Tests.Features.Articles.Commands;

using System.Linq.Expressions;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.PublishArticle;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class PublishArticleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock = new();
    private readonly Mock<ILogger<PublishArticleCommandHandler>> _loggerMock = new();

    private PublishArticleCommandHandler CreateHandler() =>
        new(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _cacheServiceMock.Object,
            _notificationServiceMock.Object,
            _backgroundJobServiceMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenArticleIsDraft_ShouldPublishSuccessfully()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo A", Email = "author@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Tin nóng",
            Slug = "tin-nong",
            ContentHtml = "<p>Nội dung bài viết</p>",
            Status = ArticleStatus.Draft
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new PublishArticleCommand(articleId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
        Assert.NotNull(result.PublishedAt);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyArticleStatusChangedAsync(authorId, articleId, "Published", It.IsAny<CancellationToken>()), Times.Once);
        _backgroundJobServiceMock.Verify(b => b.Enqueue(It.IsAny<Expression<Func<INotificationService, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenArticleAlreadyPublished_ShouldBeIdempotentWithoutResendingNotifications()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var initialPublishedAt = DateTimeOffset.UtcNow.AddHours(-2);
        var author = new User { Id = authorId, FullName = "Nhà Báo A", Email = "author@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Tin nóng đã xuất bản",
            Slug = "tin-nong-da-xuat-ban",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            PublishedAt = initialPublishedAt
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new PublishArticleCommand(articleId), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
        Assert.Equal(initialPublishedAt, result.PublishedAt); // Does not overwrite PublishedAt
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _notificationServiceMock.Verify(n => n.NotifyArticleStatusChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSignalRThrows_ShouldCatchAndSucceed()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo A", Email = "author@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Tin nóng",
            Slug = "tin-nong",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Draft
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _notificationServiceMock
            .Setup(n => n.NotifyArticleStatusChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SignalR hub disconnected"));

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new PublishArticleCommand(articleId), CancellationToken.None);

        // Assert - Command succeeds despite SignalR transient failure
        Assert.NotNull(result);
        Assert.Equal("Published", result.Status);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAuthorAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var strangerId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo A", Email = "author@newshub.vn", PasswordHash = "hashed" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Tin nóng",
            Slug = "tin-nong",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Draft
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(strangerId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new PublishArticleCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleForbiddenEdit, ex.ErrorCode);
    }
}
