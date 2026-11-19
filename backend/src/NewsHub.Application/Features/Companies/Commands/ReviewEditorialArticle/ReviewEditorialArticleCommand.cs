namespace NewsHub.Application.Features.Companies.Commands.ReviewEditorialArticle;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.Constants;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record ReviewEditorialArticleCommand(
    Guid CompanyId,
    Guid ArticleId,
    string Action,
    string? EditorialNotes = null) : IRequest, ITransactionalRequest;

public sealed class ReviewEditorialArticleCommandValidator : AbstractValidator<ReviewEditorialArticleCommand>
{
    public ReviewEditorialArticleCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");

        RuleFor(x => x.Action)
            .NotEmpty().WithMessage("Hành động kiểm duyệt không được để trống.")
            .Must(a => a is "Approve" or "RequestChanges" or "Reject")
            .WithMessage("Hành động kiểm duyệt chỉ chấp nhận 'Approve', 'RequestChanges' hoặc 'Reject'.");

        When(x => x.Action is "RequestChanges" or "Reject", () =>
        {
            RuleFor(x => x.EditorialNotes)
                .NotEmpty().WithMessage("Vui lòng cung cấp ghi chú biên tập khi yêu cầu chỉnh sửa hoặc từ chối bài viết.")
                .MaximumLength(1000).WithMessage("Ghi chú biên tập không được vượt quá 1000 ký tự.");
        });
    }
}

public sealed class ReviewEditorialArticleCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    ICacheService cacheService,
    INotificationService notificationService,
    ILogger<ReviewEditorialArticleCommandHandler> logger,
    IBackgroundJobService? backgroundJobService = null) : IRequestHandler<ReviewEditorialArticleCommand>
{
    public async Task Handle(ReviewEditorialArticleCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền thực hiện thao tác này.", ErrorCodes.PermissionDenied);
        }

        var currentUserId = currentUser.Id!.Value;

        var isOwner = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (!isOwner)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền kiểm duyệt bài viết.", ErrorCodes.OnlyOwnerAllowed);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        var article = await dbContext.Articles
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId && a.CompanyId == request.CompanyId, cancellationToken);

        if (article == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết trong tòa soạn này.", ErrorCodes.ArticleNotFound);
        }

        if (article.Status != ArticleStatus.PendingCompanyReview)
        {
            throw new BadRequestException("Bài viết không ở trạng thái chờ tòa soạn phê duyệt.", ErrorCodes.ArticleNotSubmittedToCompany);
        }

        if (request.Action == "Approve")
        {
            article.Status = ArticleStatus.Published;
            article.PublishedAt = DateTimeOffset.UtcNow;
            article.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Bài viết đã được cập nhật bởi một phiên làm việc khác.", ErrorCodes.Conflict);
            }

            // Vô hiệu hóa cache bài viết và overview
            try
            {
                await cacheService.RemoveAsync(ArticleCacheKeys.ArticleSlug(article.Slug), cancellationToken);
                await cacheService.RemoveAsync(ArticleCacheKeys.ArticleDetail(article.Id), cancellationToken);
                await cacheService.RemoveAsync(ArticleCacheKeys.ArticleRelated(article.Id), cancellationToken);
                await cacheService.RemoveAsync(AdminCacheKeys.AnalyticsOverview, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi dọn cache sau khi xuất bản bài viết {ArticleId}.", article.Id);
            }

            logger.LogInformation("Owner {OwnerId} đã phê duyệt xuất bản bài viết {ArticleId} của tòa soạn {CompanyId}.",
                currentUserId, article.Id, request.CompanyId);

            // Gửi SignalR tới tác giả
            try
            {
                await notificationService.NotifyArticleStatusChangedAsync(
                    article.AuthorId,
                    article.Id,
                    ArticleStatus.Published.ToString(),
                    cancellationToken);

                // Gửi SignalR phát sóng tới nhóm theo dõi tòa soạn
                var followerNotification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.CompanyArticlePublished,
                    $"Bài viết mới từ {company.Name}",
                    $"Tòa soạn {company.Name} vừa xuất bản bài viết mới: \"{article.Title}\".",
                    article.Id,
                    $"/articles/{article.Slug}"
                );
                await notificationService.SendToGroupAsync($"Company_{company.Id}", followerNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi gửi SignalR notification sau khi xuất bản bài viết {ArticleId}.", article.Id);
            }

            try
            {
                // Kích hoạt tác vụ nền Hangfire xử lý tóm tắt AI
                backgroundJobService?.Enqueue<IAiBackgroundJobs>(job =>
                    job.GenerateArticleSummaryAsync(article.Id, CancellationToken.None));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi đưa tác vụ tóm tắt AI vào hàng đợi cho bài viết {ArticleId}", article.Id);
            }

            try
            {
                // Kích hoạt tác vụ nền Hangfire sinh vector nhúng
                backgroundJobService?.Enqueue<IAiBackgroundJobs>(job =>
                    job.GenerateArticleEmbeddingAsync(article.Id, CancellationToken.None));
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi khi đưa tác vụ sinh vector nhúng vào hàng đợi cho bài viết {ArticleId}", article.Id);
            }
        }
        else if (request.Action == "RequestChanges")
        {
            article.Status = ArticleStatus.Draft;
            article.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Bài viết đã được cập nhật bởi một phiên làm việc khác.", ErrorCodes.Conflict);
            }

            logger.LogInformation("Owner {OwnerId} đã yêu cầu tác giả sửa đổi bài viết {ArticleId}.", currentUserId, article.Id);

            try
            {
                var authorNotification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.ArticleStatusChanged,
                    "Yêu cầu chỉnh sửa bài viết",
                    $"Tòa soạn {company.Name} đã yêu cầu bạn chỉnh sửa bài viết \"{article.Title}\". Ghi chú: {request.EditorialNotes}",
                    article.Id
                );
                await notificationService.SendToUserAsync(article.AuthorId, authorNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi gửi SignalR notification khi yêu cầu sửa bài viết {ArticleId}.", article.Id);
            }
        }
        else // Reject
        {
            article.Status = ArticleStatus.Rejected;
            article.UpdatedAt = DateTimeOffset.UtcNow;

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new ConflictException("Bài viết đã được cập nhật bởi một phiên làm việc khác.", ErrorCodes.Conflict);
            }

            logger.LogInformation("Owner {OwnerId} đã từ chối bài viết {ArticleId}.", currentUserId, article.Id);

            try
            {
                var authorNotification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.ArticleStatusChanged,
                    "Bài viết bị từ chối phê duyệt",
                    $"Tòa soạn {company.Name} đã từ chối bài viết \"{article.Title}\". Lý do: {request.EditorialNotes}",
                    article.Id
                );
                await notificationService.SendToUserAsync(article.AuthorId, authorNotification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi gửi SignalR notification khi từ chối bài viết {ArticleId}.", article.Id);
            }
        }
    }
}
