namespace NewsHub.Application.Features.Companies.Commands.RespondToInvitation;

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

public sealed record RespondToInvitationCommand(Guid RequestId, bool Accept) : IRequest;

public sealed class RespondToInvitationCommandValidator : AbstractValidator<RespondToInvitationCommand>
{
    public RespondToInvitationCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Định danh lời mời không được để trống.");
    }
}

public sealed class RespondToInvitationCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<RespondToInvitationCommandHandler> logger) : IRequestHandler<RespondToInvitationCommand>
{
    public async Task Handle(RespondToInvitationCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Journalist))
        {
            throw new ForbiddenException("Chỉ nhà báo mới có quyền phản hồi lời mời gia nhập.", ErrorCodes.PermissionDenied);
        }

        var userId = currentUser.Id!.Value;

        var invite = await dbContext.CompanyRequests
            .Include(r => r.Company)
            .Include(r => r.Journalist)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.JournalistId == userId && r.Type == CompanyRequestType.Invitation, cancellationToken);

        if (invite == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy lời mời gia nhập tương ứng.", ErrorCodes.RequestNotFound);
        }

        if (invite.Status != CompanyRequestStatus.Pending)
        {
            throw new BadRequestException("Lời mời này đã được xử lý trước đó.", ErrorCodes.InvalidRequestStatus);
        }

        if (request.Accept)
        {
            // Kiểm tra giới hạn 5 công ty
            var activeCount = await dbContext.CompanyMembers
                .CountAsync(m => m.UserId == userId, cancellationToken);
            if (activeCount >= 5)
            {
                throw new BadRequestException("Bạn đã tham gia tối đa 5 tòa soạn. Không thể chấp nhận thêm lời mời.", ErrorCodes.MaxCompaniesLimitReached);
            }

            var isAlreadyMember = await dbContext.CompanyMembers
                .AnyAsync(m => m.CompanyId == invite.CompanyId && m.UserId == userId, cancellationToken);

            if (!isAlreadyMember)
            {
                var member = new CompanyMember
                {
                    CompanyId = invite.CompanyId,
                    UserId = userId,
                    Role = CompanyMemberRole.Member,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                dbContext.CompanyMembers.Add(member);
            }

            invite.Status = CompanyRequestStatus.Accepted;
            invite.RespondedAt = DateTimeOffset.UtcNow;

            logger.LogInformation("Nhà báo {UserId} đã chấp nhận lời mời gia nhập công ty {CompanyId}.", userId, invite.CompanyId);

            // Bắn SignalR tới Owner của công ty
            if (invite.Company != null)
            {
                var notification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.CompanyInvitation,
                    "Lời mời đã được chấp nhận",
                    $"Nhà báo {invite.Journalist?.FullName ?? "Thành viên mới"} đã chấp nhận lời mời gia nhập tòa soạn {invite.Company.Name}.",
                    invite.CompanyId
                );
                await notificationService.SendToUserAsync(invite.Company.OwnerId, notification, cancellationToken);
            }
        }
        else
        {
            invite.Status = CompanyRequestStatus.Rejected;
            invite.RespondedAt = DateTimeOffset.UtcNow;
            logger.LogInformation("Nhà báo {UserId} đã từ chối lời mời gia nhập công ty {CompanyId}.", userId, invite.CompanyId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
