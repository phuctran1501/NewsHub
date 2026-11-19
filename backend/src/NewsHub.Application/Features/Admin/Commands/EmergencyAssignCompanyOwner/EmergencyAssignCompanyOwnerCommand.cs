namespace NewsHub.Application.Features.Admin.Commands.EmergencyAssignCompanyOwner;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record EmergencyAssignCompanyOwnerCommand(
    Guid CompanyId,
    Guid NewOwnerId,
    string Reason) : IRequest, ITransactionalRequest;

public sealed class EmergencyAssignCompanyOwnerCommandValidator : AbstractValidator<EmergencyAssignCompanyOwnerCommand>
{
    public EmergencyAssignCompanyOwnerCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty().WithMessage("Định danh chủ sở hữu mới không được để trống.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Lý do chỉ định Owner khẩn cấp không được để trống.")
            .MaximumLength(1000).WithMessage("Lý do không được vượt quá 1000 ký tự.");
    }
}

public sealed class EmergencyAssignCompanyOwnerCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<EmergencyAssignCompanyOwnerCommandHandler> logger) : IRequestHandler<EmergencyAssignCompanyOwnerCommand>
{
    public async Task Handle(EmergencyAssignCompanyOwnerCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền chỉ định Owner thay thế khẩn cấp.", ErrorCodes.PermissionDenied);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        var newOwnerMember = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == request.NewOwnerId, cancellationToken);

        if (newOwnerMember == null)
        {
            throw new BadRequestException("Chủ sở hữu mới được chỉ định phải đang là thành viên của tòa soạn.", ErrorCodes.NotCompanyMember);
        }

        var oldOwnerId = company.OwnerId;

        // Nếu có member record của Owner cũ thì hạ xuống Member
        var oldOwnerMember = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == oldOwnerId, cancellationToken);

        if (oldOwnerMember != null)
        {
            oldOwnerMember.Role = CompanyMemberRole.Member;
        }

        newOwnerMember.Role = CompanyMemberRole.Owner;
        company.OwnerId = request.NewOwnerId;
        company.UpdatedAt = DateTimeOffset.UtcNow;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("Quyền sở hữu tòa soạn đã bị thay đổi bởi một thao tác khác.", ErrorCodes.Conflict);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("Tòa soạn đã có chủ sở hữu khác được cập nhật đồng thời.", ErrorCodes.Conflict);
        }

        logger.LogWarning("Admin đã kích hoạt quyền can thiệp khẩn cấp: Chỉ định {NewOwnerId} làm Owner mới của tòa soạn {CompanyId}. Lý do: {Reason}",
            request.NewOwnerId, request.CompanyId, request.Reason);

        // Bắn SignalR notification tới Owner mới
        try
        {
            var notification = new NotificationDto(
                Guid.CreateVersion7(),
                NotificationType.CompanyMembershipChanged,
                "Chỉ định tiếp quản Tòa soạn khẩn cấp",
                $"Quản trị viên đã chỉ định bạn làm Chủ sở hữu (Owner) của tòa soạn {company.Name}. Lý do: {request.Reason}",
                company.Id
            );
            await notificationService.SendToUserAsync(request.NewOwnerId, notification, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Không thể gửi SignalR notification tới Owner mới {NewOwnerId}.", request.NewOwnerId);
        }
    }
}
