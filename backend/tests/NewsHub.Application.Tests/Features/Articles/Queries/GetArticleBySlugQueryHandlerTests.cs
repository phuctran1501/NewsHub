namespace NewsHub.Application.Tests.Features.Articles.Queries;

using MediatR;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Application.Features.Articles.Queries.GetArticleBySlug;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetArticleBySlugQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ISender> _senderMock = new();
    private readonly Mock<ILogger<GetArticleBySlugQueryHandler>> _loggerMock = new();

    private GetArticleBySlugQueryHandler CreateHandler() =>
        new(
            _dbContextMock.Object,
            _cacheServiceMock.Object,
            _currentUserMock.Object,
            _senderMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenArticleExistsAndPublished_ShouldReturnDetailAndCacheBothKeys()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo B", Email = "b@newshub.vn", PasswordHash = "hash" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Công nghệ AI mới",
            Slug = "cong-nghe-ai-moi",
            ContentHtml = "<p>Nội dung chi tiết</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        var histories = new List<ReadingHistory>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetArticleBySlugQuery("cong-nghe-ai-moi"), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("cong-nghe-ai-moi", result.Slug);
        Assert.Equal(articleId, result.Id);
        _cacheServiceMock.Verify(c => c.SetAsync("articles:slug:cong-nghe-ai-moi", It.IsAny<ArticleDetailDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.SetAsync($"articles:detail:{articleId}", It.IsAny<ArticleDetailDto>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenQueryingByGuid_ShouldUseArticleDetailCacheKey()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var author = new User { Id = authorId, FullName = "Nhà Báo B", Email = "b@newshub.vn", PasswordHash = "hash" };
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Author = author,
            Title = "Công nghệ AI mới",
            Slug = "cong-nghe-ai-moi",
            ContentHtml = "<p>Nội dung chi tiết</p>",
            Status = ArticleStatus.Published,
            PublishedAt = DateTimeOffset.UtcNow
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        var histories = new List<ReadingHistory>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.ReadingHistories).Returns(histories.Object);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetArticleBySlugQuery(articleId.ToString()), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(articleId, result.Id);
        // Verified: The first cache lookup used articles:detail:{id} rather than articles:slug:{guid}
        _cacheServiceMock.Verify(c => c.GetAsync<ArticleDetailDto>($"articles:detail:{articleId}", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenArticleDraftAndUserIsGuest_ShouldThrowNotFoundException()
    {
        // Arrange
        var article = new Article
        {
            Id = Guid.NewGuid(),
            AuthorId = Guid.NewGuid(),
            Title = "Bản nháp bí mật",
            Slug = "ban-nhap-bi-mat",
            ContentHtml = "<p>Nháp</p>",
            Status = ArticleStatus.Draft
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetArticleBySlugQuery("ban-nhap-bi-mat"), CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleNotFound, ex.ErrorCode);
    }
}
