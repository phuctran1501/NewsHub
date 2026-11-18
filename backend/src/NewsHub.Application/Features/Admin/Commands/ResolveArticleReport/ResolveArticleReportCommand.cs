namespace NewsHub.Application.Features.Admin.Commands.ResolveArticleReport;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record ResolveArticleReportResultDto(
    Guid ReportId,
    ReportStatus Status,
    bool ArticleArchived,
    string Message);

/// <summary>
/// Command xử lý báo cáo vi phạm bài viết (Resolved / Dismissed), có tùy chọn gỡ bài viết vi phạm sang Archived.
/// </summary>
public sealed record ResolveArticleReportCommand(
    Guid ReportId,
    ReportStatus Status,
    bool ArchiveArticle = false,
    string? AdminNote = null) : IRequest<ResolveArticleReportResultDto>;

public sealed class ResolveArticleReportCommandValidator : AbstractValidator<ResolveArticleReportCommand>
{
    public ResolveArticleReportCommandValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty().WithMessage("Định danh báo cáo vi phạm (reportId) không được để trống.");

        RuleFor(x => x.Status)
            .Must(s => s == ReportStatus.Resolved || s == ReportStatus.Dismissed)
            .WithMessage("Trạng thái xử lý chỉ có thể là Resolved hoặc Dismissed.");

        RuleFor(x => x.AdminNote)
            .MaximumLength(500).WithMessage("Ghi chú của quản trị viên không được vượt quá 500 ký tự.");
    }
}

public sealed class ResolveArticleReportCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    INotificationService notificationService) : IRequestHandler<ResolveArticleReportCommand, ResolveArticleReportResultDto>
{
    public async Task<ResolveArticleReportResultDto> Handle(
        ResolveArticleReportCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền xử lý báo cáo vi phạm.", ErrorCodes.PermissionDenied);
        }

        var report = await dbContext.ArticleReports
            .Include(r => r.Article)
            .FirstOrDefaultAsync(r => r.Id == request.ReportId, cancellationToken);

        if (report is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy báo cáo vi phạm.", ErrorCodes.ReportNotFound);
        }

        var now = DateTimeOffset.UtcNow;
        report.Status = request.Status;
        report.ResolvedAt = now;
        report.ResolvedBy = currentUser.Id;

        if (!string.IsNullOrWhiteSpace(request.AdminNote))
        {
            report.Details = string.IsNullOrWhiteSpace(report.Details)
                ? $"[Admin Note]: {request.AdminNote.Trim()}"
                : $"{report.Details}\n[Admin Note]: {request.AdminNote.Trim()}";
        }

        var articleArchived = false;

        // Nếu quản trị viên chọn gỡ bài viết khi xử lý vi phạm (Resolved)
        if (request.ArchiveArticle && request.Status == ReportStatus.Resolved && report.Article is not null)
        {
            report.Article.Status = ArticleStatus.Archived;
            report.Article.UpdatedAt = now;
            articleArchived = true;

            // Xóa cache của bài viết và overview
            await cacheService.RemoveAsync($"article:{report.Article.Slug}", cancellationToken);
            await cacheService.RemoveAsync($"article:id:{report.Article.Id}", cancellationToken);
            await cacheService.RemoveByPrefixAsync("articles:", cancellationToken);
            await cacheService.RemoveAsync(AdminCacheKeys.AnalyticsOverview, cancellationToken);

            // Gửi thông báo SignalR tới tác giả bài viết
            await notificationService.NotifyArticleStatusChangedAsync(
                report.Article.AuthorId,
                report.Article.Id,
                ArticleStatus.Archived.ToString(),
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        var message = articleArchived
            ? "Đã xử lý báo cáo vi phạm và gỡ bài viết thành công."
            : "Đã xử lý báo cáo vi phạm thành công.";

        return new ResolveArticleReportResultDto(
            report.Id,
            report.Status,
            articleArchived,
            message);
    }
}
