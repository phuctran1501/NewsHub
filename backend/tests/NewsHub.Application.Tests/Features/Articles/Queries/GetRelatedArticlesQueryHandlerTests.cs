namespace NewsHub.Application.Tests.Features.Articles.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Application.Features.Articles.Queries.GetRelatedArticles;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Xunit;

public class GetRelatedArticlesQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();

    private GetRelatedArticlesQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _cacheServiceMock.Object);

    [Fact]
    public async Task Handle_WhenCacheContainsMultipleArticles_ShouldApplyCallerLimit()
    {
        // Arrange
        var targetArticleId = Guid.NewGuid();
        var authorDto = new AuthorSummaryDto(Guid.NewGuid(), "Tác giả", null);

        var cachedArticles = Enumerable.Range(1, 10).Select(i => new ArticleSummaryDto(
            Guid.NewGuid(),
            $"Bài viết liên quan {i}",
            $"bai-viet-lien-quan-{i}",
            "Tóm tắt",
            "Published",
            authorDto,
            null,
            null,
            3,
            100,
            new List<CategorySummaryDto>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow)).ToList();

        // Simulate cache returning 10 cached related articles
        _cacheServiceMock
            .Setup(c => c.GetOrCreateAsync(
                ArticleCacheKeys.ArticleRelated(targetArticleId),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<ArticleSummaryDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedArticles);

        var handler = CreateHandler();

        // Act - Requesting only 3 related articles
        var result = await handler.Handle(new GetRelatedArticlesQuery(targetArticleId, 3), CancellationToken.None);

        // Assert - Only 3 items returned despite cache containing 10
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Equal("Bài viết liên quan 1", result[0].Title);
        Assert.Equal("Bài viết liên quan 3", result[2].Title);
    }
}
