namespace NewsHub.Application.Tests.Features.Admin.Queries;

using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Queries.GetArticleReports;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class GetArticleReportsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetArticleReportsQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetArticleReportsQuery(), CancellationToken.None));
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
            handler.Handle(new GetArticleReportsQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValidQuery_ShouldReturnPagedReports()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Bài báo cần xem xét",
            Slug = "bai-bao-can-xem-xet",
            ContentHtml = "<p>Nội dung</p>"
        };

        var reporter = new User
        {
            Id = Guid.NewGuid(),
            Email = new Email("reporter@example.com"),
            FullName = "Độc Giả Báo Cáo",
            PasswordHash = "hash"
        };

        var reports = new List<ArticleReport>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = article.Id,
                Article = article,
                ReporterId = reporter.Id,
                Reporter = reporter,
                Reason = ReportReason.Misinformation,
                Details = "Thông tin sai sự thật",
                Status = ReportStatus.Pending,
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        _dbContextMock.Setup(db => db.ArticleReports).Returns(reports.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetArticleReportsQuery(1, 10), CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Bài báo cần xem xét", result.Items[0].ArticleTitle);
        Assert.Equal("Độc Giả Báo Cáo", result.Items[0].Reporter?.FullName);
        Assert.Equal(ReportReason.Misinformation, result.Items[0].Reason);
    }

    [Fact]
    public async Task Handle_WhenFilteringByStatusAndArticleId_ShouldReturnMatchingReports()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        var targetArticleId = Guid.NewGuid();
        var reports = new List<ArticleReport>
        {
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = targetArticleId,
                Reason = ReportReason.Spam,
                Status = ReportStatus.Pending
            },
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = targetArticleId,
                Reason = ReportReason.CopyrightViolation,
                Status = ReportStatus.Resolved
            },
            new()
            {
                Id = Guid.NewGuid(),
                ArticleId = Guid.NewGuid(),
                Reason = ReportReason.Spam,
                Status = ReportStatus.Pending
            }
        };

        _dbContextMock.Setup(db => db.ArticleReports).Returns(reports.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act: Filter by Status = Pending and ArticleId = targetArticleId
        var result = await handler.Handle(new GetArticleReportsQuery(1, 10, Status: ReportStatus.Pending, ArticleId: targetArticleId), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(targetArticleId, result.Items[0].ArticleId);
        Assert.Equal(ReportStatus.Pending, result.Items[0].Status);
    }
}
