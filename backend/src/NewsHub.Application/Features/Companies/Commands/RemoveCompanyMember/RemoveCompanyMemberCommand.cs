namespace NewsHub.Application.Features.Companies.Commands.RemoveCompanyMember;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record RemoveCompanyMemberCommand(Guid CompanyId, Guid UserId) : IRequest;

public sealed class RemoveCompanyMemberCommandValidator : AbstractValidator<RemoveCompanyMemberCommand>
{
    public RemoveCompanyMemberCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Định danh thành viên cần xóa không được để trống.");
    }
}

public sealed class RemoveCompanyMemberCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<RemoveCompanyMemberCommandHandler> logger) : IRequestHandler<RemoveCompanyMemberCommand>
{
    public async Task Handle(RemoveCompanyMemberCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền xóa thành viên.", ErrorCodes.OnlyOwnerAllowed);
        }

        if (request.UserId == currentUserId)
        {
            throw new BadRequestException("Chủ sở hữu không thể tự xóa chính mình khỏi danh sách nhân viên.", ErrorCodes.CannotRemoveSelf);
        }

        var member = await dbContext.CompanyMembers
            .Include(m => m.Company)
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == request.UserId, cancellationToken);

        if (member == null)
        {
            throw NotFoundException.WithErrorCode("Thành viên không thuộc tòa soạn này.", ErrorCodes.NotCompanyMember);
        }

        dbContext.CompanyMembers.Remove(member);

        // Tự động giải phóng các bài viết nháp (Draft) và chờ duyệt (PendingCompanyReview) của thành viên bị xóa về cá nhân
        await dbContext.Articles
            .Where(a => a.AuthorId == request.UserId && a.CompanyId == request.CompanyId &&
                        (a.Status == ArticleStatus.Draft || a.Status == ArticleStatus.PendingCompanyReview))
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.CompanyId, (Guid?)null)
                .SetProperty(a => a.Status, ArticleStatus.Draft)
                .SetProperty(a => a.UpdatedAt, DateTimeOffset.UtcNow), cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Owner {CurrentUserId} đã xóa thành viên {UserId} khỏi công ty {CompanyId}. Các bài nháp và chờ duyệt đã được chuyển về cá nhân.",
            currentUserId, request.UserId, request.CompanyId);

        // Bắn SignalR thông báo tới thành viên bị xóa
        var companyName = member.Company?.Name ?? "Tòa soạn";
        var notification = new NotificationDto(
            Guid.CreateVersion7(),
            NotificationType.CompanyMembershipChanged,
            "Đã rời khỏi Tòa soạn",
            $"Bạn đã được đưa ra khỏi danh sách nhân sự của {companyName}. Các bài viết nháp và chờ duyệt của bạn đã được chuyển về trang cá nhân.",
            request.CompanyId
        );
        await notificationService.SendToUserAsync(request.UserId, notification, cancellationToken);
    }
}
