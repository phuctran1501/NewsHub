namespace NewsHub.Application.Features.Articles.Commands.ReportArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record ReportArticleCommand(
    Guid ArticleId,
    ReportReason Reason,
    string? Details = null) : IRequest<Unit>;

public sealed class ReportArticleCommandValidator : AbstractValidator<ReportArticleCommand>
{
    public ReportArticleCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");

        RuleFor(x => x.Reason)
            .IsInEnum().WithMessage("Lý do báo cáo vi phạm không hợp lệ.");

        When(x => !string.IsNullOrEmpty(x.Details), () =>
        {
            RuleFor(x => x.Details!)
                .MaximumLength(1000).WithMessage("Chi tiết báo cáo không được vượt quá 1000 ký tự.");
        });
    }
}

public sealed class ReportArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    ICacheService cacheService,
    ILogger<ReportArticleCommandHandler> logger) : IRequestHandler<ReportArticleCommand, Unit>
{
    private const int MaxReportsPerHour = 5;

    public async Task<Unit> Handle(
        ReportArticleCommand request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để gửi báo cáo vi phạm bài viết.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id!.Value;

        // 1. Kiểm tra Rate Limit qua Cache (5 req/h)
        try
        {
            var rateLimitKey = ArticleCacheKeys.ReportRateLimit(userId);
            var currentCount = await cacheService.IncrementAsync(rateLimitKey, TimeSpan.FromHours(1), cancellationToken);

            if (currentCount > MaxReportsPerHour)
            {
                throw new ConflictException("Bạn đã vượt quá giới hạn 5 lượt gửi báo cáo trong vòng 1 giờ. Vui lòng thử lại sau.", ErrorCodes.RateLimitExceeded);
            }
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi kiểm tra rate limit cho UserId: {UserId}", userId);
        }

        // 2. Kiểm tra bài viết tồn tại
        var articleExists = await dbContext.Articles
            .AnyAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (!articleExists)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết yêu cầu.", ErrorCodes.ArticleNotFound);
        }

        // 3. Kiểm tra xem người dùng đã báo cáo bài viết này chưa
        var alreadyReported = await dbContext.ArticleReports
            .AnyAsync(r => r.ArticleId == request.ArticleId &&
                           r.ReporterId == userId &&
                           r.Status == ReportStatus.Pending, cancellationToken);

        if (alreadyReported)
        {
            throw new ConflictException("Bạn đã gửi báo cáo cho bài viết này và đang chờ xử lý.", ErrorCodes.ReportAlreadySubmitted);
        }

        var report = new ArticleReport
        {
            ArticleId = request.ArticleId,
            ReporterId = userId,
            Reason = request.Reason,
            Details = string.IsNullOrWhiteSpace(request.Details) ? null : request.Details.Trim(),
            Status = ReportStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.ArticleReports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
