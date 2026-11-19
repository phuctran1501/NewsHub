namespace NewsHub.Application.Features.Admin.Commands.ReviewCompanyStatus;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record ReviewCompanyStatusCommand(
    Guid CompanyId,
    string Status,
    string? RejectionReason = null) : IRequest;

public sealed class ReviewCompanyStatusCommandValidator : AbstractValidator<ReviewCompanyStatusCommand>
{
    public ReviewCompanyStatusCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage("Trạng thái phê duyệt không được để trống.")
            .Must(s => s is "Approved" or "Rejected")
            .WithMessage("Trạng thái chỉ chấp nhận 'Approved' hoặc 'Rejected'.");

        When(x => x.Status == "Rejected", () =>
        {
            RuleFor(x => x.RejectionReason)
                .NotEmpty().WithMessage("Vui lòng cung cấp lý do từ chối thành lập tòa soạn.")
                .MaximumLength(1000).WithMessage("Lý do từ chối không được vượt quá 1000 ký tự.");
        });
    }
}

public sealed class ReviewCompanyStatusCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<ReviewCompanyStatusCommandHandler> logger) : IRequestHandler<ReviewCompanyStatusCommand>
{
    public async Task Handle(ReviewCompanyStatusCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền thẩm định tòa soạn.", ErrorCodes.PermissionDenied);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        if (company.Status != CompanyStatus.Pending)
        {
            throw new BadRequestException("Tòa soạn này không ở trạng thái chờ phê duyệt.", ErrorCodes.InvalidCompanyStatus);
        }

        if (request.Status == "Approved")
        {
            company.Status = CompanyStatus.Approved;
            company.UpdatedAt = DateTimeOffset.UtcNow;

            // Tự động tạo bản ghi CompanyMember cho người sáng lập với vai trò Owner
            var isAlreadyMember = await dbContext.CompanyMembers
                .AnyAsync(m => m.CompanyId == company.Id && m.UserId == company.OwnerId, cancellationToken);

            if (!isAlreadyMember)
            {
                var ownerMember = new CompanyMember
                {
                    CompanyId = company.Id,
                    UserId = company.OwnerId,
                    Role = CompanyMemberRole.Owner,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                dbContext.CompanyMembers.Add(ownerMember);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Admin đã phê duyệt thành lập tòa soạn {CompanyId} ({CompanyName}).", company.Id, company.Name);

            // Bắn SignalR thông báo tới người sáng lập
            var notification = new NotificationDto(
                Guid.CreateVersion7(),
                NotificationType.CompanyMembershipChanged,
                "Tòa soạn đã được phê duyệt",
                $"Chúc mừng! Hồ sơ thành lập tòa soạn {company.Name} của bạn đã được Quản trị viên phê duyệt chính thức.",
                company.Id
            );
            await notificationService.SendToUserAsync(company.OwnerId, notification, cancellationToken);
        }
        else // Rejected
        {
            company.Status = CompanyStatus.Rejected;
            company.UpdatedAt = DateTimeOffset.UtcNow;

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Admin đã từ chối thành lập tòa soạn {CompanyId}. Lý do: {Reason}", company.Id, request.RejectionReason);

            var notification = new NotificationDto(
                Guid.CreateVersion7(),
                NotificationType.CompanyMembershipChanged,
                "Hồ sơ thành lập tòa soạn bị từ chối",
                $"Hồ sơ thành lập tòa soạn {company.Name} của bạn đã bị từ chối. Lý do: {request.RejectionReason}",
                company.Id
            );
            await notificationService.SendToUserAsync(company.OwnerId, notification, cancellationToken);
        }
    }
}
