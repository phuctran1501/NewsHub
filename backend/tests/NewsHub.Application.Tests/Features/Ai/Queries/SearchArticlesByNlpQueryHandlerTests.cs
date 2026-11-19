namespace NewsHub.Application.Tests.Features.Ai.Queries;

using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp;
using Pgvector;
using Xunit;

public class SearchArticlesByNlpQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();

    private SearchArticlesByNlpQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _aiServiceMock.Object);

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldGenerateEmbeddingAndQueryDbContext()
    {
        // Arrange
        var queryText = "chiến lược phát triển kinh tế số";
        var dummyEmbedding = new float[768];
        dummyEmbedding[0] = 0.5f;

        _aiServiceMock.Setup(a => a.GenerateEmbeddingAsync(queryText, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dummyEmbedding);

        var articleId = Guid.NewGuid();
        var expectedResults = new List<SemanticSearchResultDto>
        {
            new(articleId, "Đẩy mạnh chuyển đổi số quốc gia", "chuyen-doi-so-quoc-gia", 0.88f, DateTimeOffset.UtcNow)
        };

        _dbContextMock.Setup(db => db.SearchArticlesByEmbeddingAsync(
            It.IsAny<Vector>(),
            10,
            0.65,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResults);

        var handler = CreateHandler();
        var query = new SearchArticlesByNlpQuery(queryText, 10);

        // Act
        var results = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(results);
        Assert.Single(results);
        Assert.Equal("Đẩy mạnh chuyển đổi số quốc gia", results[0].Title);
        Assert.Equal(0.88f, results[0].SimilarityScore);

        _aiServiceMock.Verify(a => a.GenerateEmbeddingAsync(queryText, It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.SearchArticlesByEmbeddingAsync(
            It.IsAny<Vector>(),
            10,
            0.65,
            It.IsAny<CancellationToken>()), Times.Once);
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
