namespace NewsHub.Application.Tests.Features.Ai.Commands;

using System.Linq.Expressions;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.Commands.RegenerateArticleAiSummary;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RegenerateArticleAiSummaryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IBackgroundJobService> _backgroundJobServiceMock = new();

    private RegenerateArticleAiSummaryCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _backgroundJobServiceMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        var handler = CreateHandler();
        var command = new RegenerateArticleAiSummaryCommand(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenArticleNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        var emptyArticles = new List<Article>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(emptyArticles.Object);

        var handler = CreateHandler();
        var command = new RegenerateArticleAiSummaryCommand(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthorAndNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(otherUserId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article>
        {
            new() { Id = articleId, AuthorId = authorId, Title = "Bài viết", Slug = "bai-viet", ContentHtml = "<p>Nội dung</p>" }
        }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();
        var command = new RegenerateArticleAiSummaryCommand(articleId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsAuthor_ShouldEnqueueHangfireJob()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article>
        {
            new() { Id = articleId, AuthorId = authorId, Title = "Bài viết", Slug = "bai-viet", ContentHtml = "<p>Nội dung</p>" }
        }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();
        var command = new RegenerateArticleAiSummaryCommand(articleId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _backgroundJobServiceMock.Verify(b => b.Enqueue(It.IsAny<Expression<Func<IAiBackgroundJobs, Task>>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserIsAdmin_ShouldEnqueueHangfireJobEvenIfNotAuthor()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(adminId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(true);

        var articles = new List<Article>
        {
            new() { Id = articleId, AuthorId = authorId, Title = "Bài viết", Slug = "bai-viet", ContentHtml = "<p>Nội dung</p>" }
        }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();
        var command = new RegenerateArticleAiSummaryCommand(articleId);

        // Act
        await handler.Handle(command, CancellationToken.None);

        // Assert
        _backgroundJobServiceMock.Verify(b => b.Enqueue(It.IsAny<Expression<Func<IAiBackgroundJobs, Task>>>()), Times.Once);
    }

    [Fact]
    public void Validator_WhenArticleIdIsEmpty_ShouldFail()
    {
        var validator = new RegenerateArticleAiSummaryCommandValidator();
        var command = new RegenerateArticleAiSummaryCommand(Guid.Empty);
        var result = validator.Validate(command);

        Assert.False(result.IsValid);
    }
}
