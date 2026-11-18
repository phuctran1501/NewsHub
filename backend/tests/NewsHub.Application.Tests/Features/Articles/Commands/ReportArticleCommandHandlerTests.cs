namespace NewsHub.Application.Tests.Features.Articles.Commands;

using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Commands.ReportArticle;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class ReportArticleCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<ILogger<ReportArticleCommandHandler>> _loggerMock = new();

    private ReportArticleCommandHandler CreateHandler() =>
        new(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _cacheServiceMock.Object,
            _loggerMock.Object);

    [Fact]
    public async Task Handle_WhenUnderRateLimit_ShouldSubmitReportSuccessfully()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var article = new Article { Id = articleId, Title = "Bài viết", Slug = "bai-viet", ContentHtml = "<p>Nội dung</p>" };

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        var articles = new List<Article> { article }.BuildMockDbSet();
        var reports = new List<ArticleReport>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Articles).Returns(articles.Object);
        _dbContextMock.Setup(db => db.ArticleReports).Returns(reports.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        // Under 5 reports
        _cacheServiceMock
            .Setup(c => c.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var handler = CreateHandler();
        var command = new ReportArticleCommand(articleId, ReportReason.Misinformation, "Tin sai sự thật");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(MediatR.Unit.Value, result);
        _dbContextMock.Verify(db => db.ArticleReports.Add(It.IsAny<ArticleReport>()), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRateLimitExceeded_ShouldThrowConflictException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();

        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(c => c.Id).Returns(userId);

        // Exceeded 5 reports
        _cacheServiceMock
            .Setup(c => c.IncrementAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(6);

        var handler = CreateHandler();
        var command = new ReportArticleCommand(articleId, ReportReason.Spam);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.RateLimitExceeded, ex.ErrorCode);
    }
}
