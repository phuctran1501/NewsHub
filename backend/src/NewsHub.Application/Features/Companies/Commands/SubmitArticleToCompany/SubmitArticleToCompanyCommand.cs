namespace NewsHub.Application.Features.Companies.Commands.SubmitArticleToCompany;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record SubmitArticleToCompanyCommand(Guid ArticleId) : IRequest, ITransactionalRequest;

public sealed class SubmitArticleToCompanyCommandValidator : AbstractValidator<SubmitArticleToCompanyCommand>
{
    public SubmitArticleToCompanyCommandValidator()
    {
        RuleFor(x => x.ArticleId)
            .NotEmpty().WithMessage("Định danh bài viết không được để trống.");
    }
}

public sealed class SubmitArticleToCompanyCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<SubmitArticleToCompanyCommandHandler> logger) : IRequestHandler<SubmitArticleToCompanyCommand>
{
    public async Task Handle(SubmitArticleToCompanyCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền nộp bài viết lên tòa soạn.", ErrorCodes.PermissionDenied);
        }

        var currentUserId = currentUser.Id!.Value;

        var article = await dbContext.Articles
            .Include(a => a.Author)
            .FirstOrDefaultAsync(a => a.Id == request.ArticleId, cancellationToken);

        if (article == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy bài viết tương ứng.", ErrorCodes.ArticleNotFound);
        }

        if (article.AuthorId != currentUserId)
        {
            throw new ForbiddenException("Chỉ tác giả mới có quyền nộp bài viết lên tòa soạn.", ErrorCodes.ArticleForbiddenEdit);
        }

        if (!article.CompanyId.HasValue)
        {
            throw new BadRequestException("Bài viết chưa được gắn với tòa soạn nào. Vui lòng chỉ định tòa soạn trước khi nộp duyệt.", ErrorCodes.ArticleHasNoCompany);
        }

        var companyId = article.CompanyId.Value;

        var isMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == companyId && m.UserId == currentUserId, cancellationToken);

        if (!isMember)
        {
            throw new ForbiddenException("Bạn không phải là thành viên của tòa soạn được gắn với bài viết này.", ErrorCodes.NotCompanyMember);
        }

        if (article.Status == ArticleStatus.Published)
        {
            throw new BadRequestException("Bài viết đã xuất bản công khai không thể nộp duyệt lại.", ErrorCodes.InvalidRequest);
        }

        if (article.Status == ArticleStatus.PendingCompanyReview)
        {
            throw new ConflictException("Bài viết đang trong trạng thái chờ tòa soạn phê duyệt.", ErrorCodes.Conflict);
        }

        if (article.Status != ArticleStatus.Draft)
        {
            throw new BadRequestException("Chỉ bài viết ở trạng thái Bản nháp (Draft) mới có thể nộp lên tòa soạn.", ErrorCodes.InvalidRequest);
        }

        article.Status = ArticleStatus.PendingCompanyReview;
        article.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Bài viết đã bị thay đổi bởi thao tác khác.", ErrorCodes.Conflict);
        }

        logger.LogInformation("Tác giả {AuthorId} đã nộp bài viết {ArticleId} lên tòa soạn {CompanyId} để chờ duyệt.",
            currentUserId, article.Id, companyId);

        try
        {
            var company = await dbContext.Companies
                .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

            if (company != null)
            {
                var notification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.ArticleStatusChanged,
                    "Bài viết mới chờ biên tập",
                    $"Nhà báo {article.Author?.FullName ?? "Phóng viên"} vừa nộp bài viết \"{article.Title}\" lên tòa soạn để chờ duyệt.",
                    article.Id
                );
                await notificationService.SendToUserAsync(company.OwnerId, notification, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Lỗi gửi SignalR notification tới Owner khi nộp bài {ArticleId}.", article.Id);
        }
    }
}
