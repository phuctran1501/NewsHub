namespace NewsHub.Application.Tests.Features.Articles.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.DeleteArticle;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class DeleteArticleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private DeleteArticleCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_WhenAuthorDeletesOwnDraftArticle_ShouldDeleteSuccessfully()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Title = "Bản nháp",
            Slug = "ban-nhap",
            ContentHtml = "<p>Nội dung</p>",
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
        await handler.Handle(new DeleteArticleCommand(articleId), CancellationToken.None);

        // Assert
        _dbContextMock.Verify(db => db.Articles.Remove(article), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"articles:slug:ban-nhap", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"articles:detail:{articleId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNonAdminDeletesPublishedCompanyArticle_ShouldThrowForbiddenException()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            CompanyId = companyId,
            Title = "Bài báo tòa soạn",
            Slug = "bai-bao-toa-soan",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteArticleCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotDeletePublishedCompanyArticle, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNonAdminDeletesArchivedCompanyArticleThatWasPublished_ShouldThrowForbiddenException()
    {
        // Arrange - Article was published earlier, then archived
        var authorId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            CompanyId = companyId,
            Title = "Bài báo tòa soạn đã lưu trữ",
            Slug = "bai-bao-toa-soan-da-luu-tru",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Archived,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-5)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(authorId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(false);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new DeleteArticleCommand(articleId), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotDeletePublishedCompanyArticle, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAdminDeletesPublishedCompanyArticle_ShouldSucceed()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            AuthorId = Guid.NewGuid(),
            CompanyId = companyId,
            Title = "Bài báo tòa soạn",
            Slug = "bai-bao-toa-soan",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(adminId);
        _currentUserMock.Setup(c => c.IsInRole(UserRole.Admin)).Returns(true);

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteArticleCommand(articleId), CancellationToken.None);

        // Assert
        _dbContextMock.Verify(db => db.Articles.Remove(article), Times.Once);
    }
}
