namespace NewsHub.Application.Tests.Features.Admin.Commands;

using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Commands.ResolveArticleReport;
using NewsHub.Application.Features.Admin.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class ResolveArticleReportCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICacheService> _cacheServiceMock = new();
    private readonly Mock<INotificationService> _notificationServiceMock = new();

    private ResolveArticleReportCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object, _cacheServiceMock.Object, _notificationServiceMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ResolveArticleReportCommand(Guid.NewGuid(), ReportStatus.Resolved), CancellationToken.None));
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
            handler.Handle(new ResolveArticleReportCommand(Guid.NewGuid(), ReportStatus.Resolved), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenReportNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);

        _dbContextMock.Setup(db => db.ArticleReports).Returns(new List<ArticleReport>().BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ResolveArticleReportCommand(Guid.NewGuid(), ReportStatus.Resolved), CancellationToken.None));
        Assert.Equal(ErrorCodes.ReportNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenDismissingReport_ShouldUpdateStatusWithoutArchivingArticle()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = "Bài viết bình thường",
            Slug = "bai-viet-binh-thuong",
            ContentHtml = "<p>Nội dung</p>",
            Status = ArticleStatus.Published
        };

        var report = new ArticleReport
        {
            Id = Guid.NewGuid(),
            ArticleId = article.Id,
            Article = article,
            Status = ReportStatus.Pending,
            Reason = ReportReason.Spam
        };

        _dbContextMock.Setup(db => db.ArticleReports).Returns(new List<ArticleReport> { report }.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new ResolveArticleReportCommand(report.Id, ReportStatus.Dismissed, ArchiveArticle: false, AdminNote: "Báo cáo không đúng"),
            CancellationToken.None);

        // Assert
        Assert.Equal(ReportStatus.Dismissed, result.Status);
        Assert.False(result.ArticleArchived);
        Assert.Equal(ReportStatus.Dismissed, report.Status);
        Assert.Equal(adminId, report.ResolvedBy);
        Assert.NotNull(report.ResolvedAt);
        Assert.Contains("[Admin Note]: Báo cáo không đúng", report.Details);
        Assert.Equal(ArticleStatus.Published, article.Status); // Vẫn giữ Published

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(n => n.NotifyArticleStatusChangedAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenResolvingReportWithArchiveArticle_ShouldArchiveArticleInvalidateCacheAndNotifyAuthor()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        var article = new Article
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Title = "Bài viết vi phạm",
            Slug = "bai-viet-vi-pham",
            ContentHtml = "<p>Nội dung xấu</p>",
            Status = ArticleStatus.Published
        };

        var report = new ArticleReport
        {
            Id = Guid.NewGuid(),
            ArticleId = article.Id,
            Article = article,
            Status = ReportStatus.Pending,
            Reason = ReportReason.CopyrightViolation
        };

        _dbContextMock.Setup(db => db.ArticleReports).Returns(new List<ArticleReport> { report }.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(
            new ResolveArticleReportCommand(report.Id, ReportStatus.Resolved, ArchiveArticle: true, AdminNote: "Xác nhận vi phạm bản quyền"),
            CancellationToken.None);

        // Assert
        Assert.Equal(ReportStatus.Resolved, result.Status);
        Assert.True(result.ArticleArchived);
        Assert.Equal(ArticleStatus.Archived, article.Status);
        Assert.NotNull(article.UpdatedAt);

        _cacheServiceMock.Verify(c => c.RemoveAsync($"article:{article.Slug}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync($"article:id:{article.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveByPrefixAsync("articles:", It.IsAny<CancellationToken>()), Times.Once);
        _cacheServiceMock.Verify(c => c.RemoveAsync(AdminCacheKeys.AnalyticsOverview, It.IsAny<CancellationToken>()), Times.Once);

        _notificationServiceMock.Verify(n => n.NotifyArticleStatusChangedAsync(
            authorId,
            article.Id,
            ArticleStatus.Archived.ToString(),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
