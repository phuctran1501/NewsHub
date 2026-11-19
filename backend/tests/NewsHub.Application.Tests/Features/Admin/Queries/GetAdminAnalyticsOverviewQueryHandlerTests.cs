namespace NewsHub.Application.Tests.Features.Admin.Queries;

using System.Linq.Expressions;
using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Constants;
using NewsHub.Application.Features.Admin.DTOs;
using NewsHub.Application.Features.Admin.Queries.GetAdminAnalyticsOverview;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class GetAdminAnalyticsOverviewQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetAdminAnalyticsOverviewQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _cacheServiceMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetAdminAnalyticsOverviewQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new GetAdminAnalyticsOverviewQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCacheHit_ShouldReturnCachedDataWithoutQueryingDb()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var cachedDto = new AdminAnalyticsOverviewDto(
            new ArticlesCountSummaryDto(10, 2, 7, 1),
            new RecentArticlesSummaryDto(3, 5),
            1200L,
            new List<TopBookmarkedArticleDto>(),
            new List<TopJournalistDto>(),
            new List<TopCategoryDto>());

        _cacheServiceMock
            .Setup(c => c.GetAsync<AdminAnalyticsOverviewDto>(AdminCacheKeys.AnalyticsOverview, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedDto);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAdminAnalyticsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.ArticlesCount.Total);
        Assert.Equal(1200L, result.TotalReads);
        _cacheServiceMock.Verify(c => c.GetAsync<AdminAnalyticsOverviewDto>(AdminCacheKeys.AnalyticsOverview, It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.Articles, Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCacheMiss_ShouldCalculateMetricsAndCacheData()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        _cacheServiceMock
            .Setup(c => c.GetAsync<AdminAnalyticsOverviewDto>(AdminCacheKeys.AnalyticsOverview, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminAnalyticsOverviewDto?)null);

        var authorId = Guid.NewGuid();
        var author = new User
        {
            Id = authorId,
            Email = new Email("author@newshub.vn"),
            FullName = "Nhà Báo A",
            PasswordHash = "hash123"
        };

        var categoryId = Guid.NewGuid();
        var category = new Category
        {
            Id = categoryId,
            Name = "Thời sự",
            Slug = "thoi-su"
        };

        var articleId = Guid.NewGuid();
        var article = new Article
        {
            Id = articleId,
            AuthorId = authorId,
            Title = "Tin tức mới nhất",
            Slug = "tin-tuc-moi-nhat",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            ViewCount = 150,
            PublishedAt = DateTimeOffset.UtcNow.AddDays(-2)
        };

        var bookmark = new UserBookmark
        {
            UserId = Guid.NewGuid(),
            ArticleId = articleId,
            Article = article,
            SavedAt = DateTimeOffset.UtcNow
        };

        var articleCategory = new ArticleCategory
        {
            ArticleId = articleId,
            Article = article,
            CategoryId = categoryId
        };

        var articles = new List<Article> { article }.BuildMockDbSet();
        var users = new List<User> { author }.BuildMockDbSet();
        var categories = new List<Category> { category }.BuildMockDbSet();
        var bookmarks = new List<UserBookmark> { bookmark }.BuildMockDbSet();
        var articleCategories = new List<ArticleCategory> { articleCategory }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.Categories).Returns(categories.Object);
        _dbContextMock.Setup(db => db.UserBookmarks).Returns(bookmarks.Object);
        _dbContextMock.Setup(db => db.ArticleCategories).Returns(articleCategories.Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetAdminAnalyticsOverviewQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.ArticlesCount.Total);
        Assert.Equal(1, result.ArticlesCount.Published);
        Assert.Equal(0, result.ArticlesCount.Draft);
        Assert.Equal(1, result.RecentArticles.Last7Days);
        Assert.Equal(1, result.RecentArticles.Last30Days);
        Assert.Equal(150L, result.TotalReads);
        Assert.Single(result.TopBookmarkedArticles);
        Assert.Equal(articleId, result.TopBookmarkedArticles[0].ArticleId);
        Assert.Single(result.TopJournalists);
        Assert.Equal("Nhà Báo A", result.TopJournalists[0].FullName);
        Assert.Single(result.TopCategories);
        Assert.Equal("Thời sự", result.TopCategories[0].Name);

        _cacheServiceMock.Verify(c => c.SetAsync(
            AdminCacheKeys.AnalyticsOverview,
            It.IsAny<AdminAnalyticsOverviewDto>(),
            AdminCacheKeys.AnalyticsOverviewTtl,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
