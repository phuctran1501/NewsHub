namespace NewsHub.Application.Tests.Features.Articles.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Queries.GetPublishedArticles;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Xunit;

public class GetPublishedArticlesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();

    private GetPublishedArticlesQueryHandler CreateHandler() => new(_dbContextMock.Object);

    [Fact]
    public async Task Handle_WithNoFilters_ShouldReturnOnlyPublishedArticles()
    {
        // Arrange
        var author = new User { Id = Guid.NewGuid(), FullName = "Tác Giả 1", Email = "a@a.com", PasswordHash = "hash" };
        var publishedArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết xuất bản",
            Slug = "bai-viet-xuat-ban",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published,
            Author = author,
            PublishedAt = DateTimeOffset.UtcNow
        };

        var draftArticle = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết nháp",
            Slug = "bai-viet-nhap",
            ContentHtml = "<p>Nội dung nháp</p>",
            Status = ArticleStatus.Draft,
            Author = author
        };

        var articles = new List<Article> { publishedArticle, draftArticle }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();
        var query = new GetPublishedArticlesQuery(1, 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("bai-viet-xuat-ban", result.Items[0].Slug);
    }

    [Fact]
    public async Task Handle_WithSortingMostRead_ShouldSortByViewCountDescending()
    {
        // Arrange
        var author = new User { Id = Guid.NewGuid(), FullName = "Tác Giả 1", Email = "a@a.com", PasswordHash = "hash" };
        var article1 = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Ít đọc",
            Slug = "it-doc",
            ContentHtml = "<p>...</p>",
            Status = ArticleStatus.Published,
            Author = author,
            ViewCount = 10,
            PublishedAt = DateTimeOffset.UtcNow
        };
        var article2 = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Nhiều đọc",
            Slug = "nhieu-doc",
            ContentHtml = "<p>...</p>",
            Status = ArticleStatus.Published,
            Author = author,
            ViewCount = 1000,
            PublishedAt = DateTimeOffset.UtcNow
        };

        var articles = new List<Article> { article1, article2 }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);

        var handler = CreateHandler();
        var query = new GetPublishedArticlesQuery(1, 10, SortBy: "mostread");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("nhieu-doc", result.Items[0].Slug);
        Assert.Equal("it-doc", result.Items[1].Slug);
    }
}
