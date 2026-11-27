namespace NewsHub.Application.Tests.Features.Ai.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Pgvector;
using Xunit;

public class SearchArticlesByNlpQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();

    private SearchArticlesByNlpQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _aiServiceMock.Object);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldUseRetrievalQueryTaskTypeAndCalibratedThreshold()
    {
        // Arrange
        var queryText = "chiến lược phát triển kinh tế số";
        var dummyEmbedding = new float[768];
        dummyEmbedding[0] = 0.5f;

        _aiServiceMock.Setup(a => a.GenerateEmbeddingAsync(
                queryText,
                EmbeddingTaskType.RetrievalQuery,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyEmbedding);

        var articleId = Guid.NewGuid();
        var expectedResults = new List<SemanticSearchResultDto>
        {
            new(articleId, "Đẩy mạnh chuyển đổi số quốc gia", "chuyen-doi-so-quoc-gia", 0.74f, DateTimeOffset.UtcNow)
        };

        _dbContextMock.Setup(db => db.SearchArticlesByEmbeddingAsync(
            It.IsAny<Vector>(),
            20,
            SearchArticlesByNlpQueryHandler.DefaultMinSimilarity,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);
        _dbContextMock.Setup(db => db.Articles)
            .Returns(new List<Article>().BuildMockDbSet().Object);

        var handler = CreateHandler();
        var query = new SearchArticlesByNlpQuery(queryText, 10);

        // Act
        var results = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Đẩy mạnh chuyển đổi số quốc gia", results[0].Title);
        Assert.Equal(0.74f, results[0].SimilarityScore);

        _aiServiceMock.Verify(a => a.GenerateEmbeddingAsync(
            queryText,
            EmbeddingTaskType.RetrievalQuery,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.SearchArticlesByEmbeddingAsync(
            It.IsAny<Vector>(),
            20,
            0.58,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_HybridSearch_ShouldBoostExactTitleMatchAndIncludeLexicalCandidate()
    {
        // Arrange
        var queryText = "hệ thống tóm tắt tin tức";
        var dummyEmbedding = new float[768];
        dummyEmbedding[0] = 1f;

        _aiServiceMock.Setup(a => a.GenerateEmbeddingAsync(
                queryText,
                EmbeddingTaskType.RetrievalQuery,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyEmbedding);

        var bothArticleId = Guid.NewGuid();
        var semanticOnlyId = Guid.NewGuid();
        var lexicalOnlyId = Guid.NewGuid();

        var semanticMatches = new List<SemanticSearchResultDto>
        {
            new(semanticOnlyId, "Mô hình ngôn ngữ lớn cho báo chí", "mo-hinh-ngon-ngu", 0.68f, DateTimeOffset.UtcNow),
            new(bothArticleId, "Liên minh châu Âu cập nhật khung minh bạch cho các hệ thống tóm tắt tin tức bằng AI", "eu-tom-tat", 0.60f, DateTimeOffset.UtcNow)
        };

        var dbArticles = new List<Article>
        {
            new()
            {
                Id = bothArticleId,
                Title = "Liên minh châu Âu cập nhật khung minh bạch cho các hệ thống tóm tắt tin tức bằng AI",
                Slug = "eu-tom-tat",
                ContentHtml = "<p>Quy định mới cho hệ thống tóm tắt tin tức.</p>",
                Status = ArticleStatus.Published,
                PublishedAt = DateTimeOffset.UtcNow
            },
            new()
            {
                Id = lexicalOnlyId,
                Title = "Xây dựng hệ thống tóm tắt tin tức thời gian thực",
                Slug = "xay-dung-tom-tat",
                ContentHtml = "<p>Bài mới vừa xuất bản chưa kịp sinh embedding.</p>",
                Status = ArticleStatus.Published,
                PublishedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
            }
        };

        _dbContextMock.Setup(db => db.SearchArticlesByEmbeddingAsync(
            It.IsAny<Vector>(),
            20,
            0.58,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(semanticMatches);
        _dbContextMock.Setup(db => db.Articles)
            .Returns(dbArticles.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act
        var results = await handler.Handle(new SearchArticlesByNlpQuery(queryText, 10), CancellationToken.None);

        // Assert: Bài khớp cả Semantic + Exact Title được đẩy lên #1 (> 0.85), bài Lexical-only vẫn xuất hiện (#2), bài Semantic-only giữ nguyên (#3)
        Assert.Equal(3, results.Count);
        Assert.Equal(bothArticleId, results[0].ArticleId);
        Assert.True(results[0].SimilarityScore > 0.85f);
        Assert.Equal(lexicalOnlyId, results[1].ArticleId);
        Assert.Equal(0.85f, results[1].SimilarityScore);
        Assert.Equal(semanticOnlyId, results[2].ArticleId);
        Assert.Equal(0.68f, results[2].SimilarityScore);
    }

    [Theory]
    [InlineData("", 10, false)]
    [InlineData("valid query", 0, false)]
    [InlineData("valid query", 25, false)]
    [InlineData("valid query", 5, true)]
    public void Validator_ValidationRules_ShouldValidateCorrectly(string query, int limit, bool expectedValid)
    {
        var validator = new SearchArticlesByNlpQueryValidator();
        var request = new SearchArticlesByNlpQuery(query, limit);
        var result = validator.Validate(request);

        Assert.Equal(expectedValid, result.IsValid);
    }
}
