namespace NewsHub.Application.Tests.Features.Ai.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Ai.Commands.GenerateArticleAiSummaryOnDemand;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GenerateArticleAiSummaryOnDemandCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IAiService> _aiServiceMock = new();

    private GenerateArticleAiSummaryOnDemandCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _aiServiceMock.Object);

    [Fact]
    public async Task Handle_WhenUnauthenticated_ShouldThrowUnauthorizedException()
    {
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);

        var handler = CreateHandler();
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GenerateArticleAiSummaryOnDemandCommand(Guid.NewGuid()), CancellationToken.None));

        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenSummaryAlreadyExists_ShouldReturnCachedSummaryWithoutCallingAi()
    {
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var articles = new List<Article>
        {
            new() { Id = articleId, Title = "Tin mới", Slug = "tin-moi", ContentHtml = "<p>Nội dung bài báo</p>", Status = ArticleStatus.Published }
        }.BuildMockDbSet();

        var existingSummaries = new List<AiSummary>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = articleId,
                BulletPoints = ["Ý chính 1", "Ý chính 2"],
                Sentiment = ArticleSentiment.Positive,
                ExtractedTags = ["Kinh tế", "AI"],
                ConfidenceScore = 0.94f,
                ProcessedAt = DateTimeOffset.UtcNow
            }
        }.BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.AiSummaries).Returns(existingSummaries.Object);

        var handler = CreateHandler();
        var result = await handler.Handle(new GenerateArticleAiSummaryOnDemandCommand(articleId), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal(2, result.BulletPoints!.Count);
        _aiServiceMock.Verify(
            ai => ai.GenerateSummaryAndSentimentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSummaryMissing_ShouldGeneratePersistAndReturnCompletedSummary()
    {
        var articleId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);

        var articles = new List<Article>
        {
            new()
            {
                Id = articleId,
                Title = "Tuyến Metro số 1",
                Slug = "metro-so-1",
                ContentHtml = "<p>Tuyến Metro số 1 giúp giảm ùn tắc giao thông cửa ngõ phía Đông.</p>",
                Status = ArticleStatus.Published
            }
        }.BuildMockDbSet();

        var emptySummaries = new List<AiSummary>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.AiSummaries).Returns(emptySummaries.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _aiServiceMock
            .Setup(ai => ai.GenerateSummaryAndSentimentAsync(
                "Tuyến Metro số 1",
                "Tuyến Metro số 1 giúp giảm ùn tắc giao thông cửa ngõ phía Đông.",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AiSummaryResultDto(
                BulletPoints: ["Giảm ùn tắc cửa ngõ phía Đông.", "Thúc đẩy giao thông công cộng."],
                Sentiment: "Positive",
                ExtractedTags: ["Metro", "TP.HCM"],
                ConfidenceScore: 0.92f,
                AiModel: "gemini-3.5-flash-lite"));

        var handler = CreateHandler();
        var result = await handler.Handle(new GenerateArticleAiSummaryOnDemandCommand(articleId), CancellationToken.None);

        Assert.Equal("Completed", result.Status);
        Assert.Equal("Positive", result.Sentiment);
        Assert.Equal(2, result.BulletPoints!.Count);
        _dbContextMock.Verify(db => db.AiSummaries.Add(It.IsAny<AiSummary>()), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
