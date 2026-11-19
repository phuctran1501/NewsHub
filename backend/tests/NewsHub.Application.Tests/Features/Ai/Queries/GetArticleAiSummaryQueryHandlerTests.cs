namespace NewsHub.Application.Tests.Features.Ai.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.Queries.GetArticleAiSummary;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetArticleAiSummaryQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();

    private GetArticleAiSummaryQueryHandler CreateHandler() =>
        new(_dbContextMock.Object);

    [Fact]
    public async Task Handle_WhenArticleDoesNotExist_ShouldThrowNotFoundException()
    {
        // Arrange
        var emptyArticles = new List<Article>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Articles).Returns(emptyArticles.Object);

        var handler = CreateHandler();
        var query = new GetArticleAiSummaryQuery(Guid.NewGuid());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.ArticleNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenArticleNotPublishedAndNoSummary_ShouldThrowNotFoundException()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var articles = new List<Article>
        {
            new() { Id = articleId, Title = "Bản nháp", Slug = "ban-nhap", ContentHtml = "<p>Nội dung</p>", Status = ArticleStatus.Draft }
        }.BuildMockDbSet();
        var summaries = new List<AiSummary>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.AiSummaries).Returns(summaries.Object);

        var handler = CreateHandler();
        var query = new GetArticleAiSummaryQuery(articleId);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(query, CancellationToken.None));
        Assert.Equal(ErrorCodes.AiSummaryNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenArticlePublishedButSummaryPending_ShouldReturnProcessingStatus()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var articles = new List<Article>
        {
            new() { Id = articleId, Title = "Đã xuất bản", Slug = "da-xuat-ban", ContentHtml = "<p>Nội dung</p>", Status = ArticleStatus.Published }
        }.BuildMockDbSet();
        var summaries = new List<AiSummary>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.AiSummaries).Returns(summaries.Object);

        var handler = CreateHandler();
        var query = new GetArticleAiSummaryQuery(articleId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Processing", result.Status);
        Assert.Null(result.BulletPoints);
    }

    [Fact]
    public async Task Handle_WhenSummaryExists_ShouldReturnCompletedSummary()
    {
        // Arrange
        var articleId = Guid.NewGuid();
        var articles = new List<Article>
        {
            new() { Id = articleId, Title = "Đã xuất bản", Slug = "da-xuat-ban", ContentHtml = "<p>Nội dung</p>", Status = ArticleStatus.Published }
        }.BuildMockDbSet();
        var now = DateTimeOffset.UtcNow;
        var summaries = new List<AiSummary>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                BulletPoints = ["Ý chính 1", "Ý chính 2"],
                Sentiment = ArticleSentiment.Positive,
                ExtractedTags = ["AI", "Tech"],
                ConfidenceScore = 0.98f,
                ProcessedAt = now
            }
        }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.AiSummaries).Returns(summaries.Object);

        var handler = CreateHandler();
        var query = new GetArticleAiSummaryQuery(articleId);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Completed", result.Status);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal(2, result.BulletPoints!.Count);
        Assert.Equal(0.98f, result.ConfidenceScore);
        Assert.Equal(now, result.ProcessedAt);
    }

    [Fact]
    public void Validator_WhenArticleIdIsEmpty_ShouldFail()
    {
        var validator = new GetArticleAiSummaryQueryValidator();
        var query = new GetArticleAiSummaryQuery(Guid.Empty);
        var result = validator.Validate(query);

        Assert.False(result.IsValid);
    }
}
