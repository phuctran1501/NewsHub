namespace NewsHub.Application.Features.Companies.Commands.InviteJournalist;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Companies.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record InviteJournalistCommand(Guid CompanyId, Guid UserId) : IRequest<CompanyRequestDto>;

public sealed class InviteJournalistCommandValidator : AbstractValidator<InviteJournalistCommand>
{
    public InviteJournalistCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Định danh nhà báo được mời không được để trống.");
    }
}

public sealed class InviteJournalistCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<InviteJournalistCommandHandler> logger) : IRequestHandler<InviteJournalistCommand, CompanyRequestDto>
{
    public async Task<CompanyRequestDto> Handle(InviteJournalistCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền gửi lời mời gia nhập.", ErrorCodes.OnlyOwnerAllowed);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        var targetUser = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (targetUser == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy nhà báo cần mời.", ErrorCodes.UserNotFound);
        }

        if (targetUser.Role != UserRole.Journalist)
        {
            throw new BadRequestException("Tài khoản này là Độc giả thông thường, không thể mời vào Tòa soạn.", ErrorCodes.UserIsNotJournalist);
        }

        var isAlreadyMember = await dbContext.CompanyMembers
            .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == targetUser.Id, cancellationToken);

        if (isAlreadyMember)
        {
            throw new ConflictException("Nhà báo này đã là thành viên của tòa soạn.", ErrorCodes.AlreadyCompanyMember);
        }

        var activeCount = await dbContext.CompanyMembers
            .CountAsync(m => m.UserId == targetUser.Id, cancellationToken);

        if (activeCount >= 5)
        {
            throw new BadRequestException("Nhà báo này đã tham gia tối đa 5 tòa soạn. Không thể mời thêm.", ErrorCodes.MaxCompaniesLimitReached);
        }

        var hasPendingInvitation = await dbContext.CompanyRequests
            .AnyAsync(r => r.CompanyId == request.CompanyId && r.JournalistId == targetUser.Id && r.Type == CompanyRequestType.Invitation && r.Status == CompanyRequestStatus.Pending, cancellationToken);

        if (hasPendingInvitation)
        {
            throw new ConflictException("Đã có lời mời đang chờ phản hồi gửi tới nhà báo này.", ErrorCodes.InvitationAlreadyPending);
        }

        var inviteRequest = new CompanyRequest
        {
            CompanyId = company.Id,
            JournalistId = targetUser.Id,
            Type = CompanyRequestType.Invitation,
            Status = CompanyRequestStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.CompanyRequests.Add(inviteRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Owner {CurrentUserId} đã gửi lời mời nhà báo {TargetUserId} gia nhập công ty {CompanyId}.",
            currentUserId, targetUser.Id, company.Id);

        // Bắn SignalR notification tới nhà báo được mời
        var notification = new NotificationDto(
            Guid.CreateVersion7(),
            NotificationType.CompanyInvitation,
            "Lời mời gia nhập Tòa soạn",
            $"Tòa soạn {company.Name} đã gửi lời mời bạn tham gia đội ngũ phóng viên.",
            company.Id
        );
        await notificationService.SendToUserAsync(targetUser.Id, notification, cancellationToken);

        return new CompanyRequestDto(
            inviteRequest.Id,
            company.Id,
            company.Name,
            company.Code,
            targetUser.Id,
            targetUser.FullName,
            (string)targetUser.Email,
            targetUser.PressCardNumber,
            inviteRequest.Type,
            inviteRequest.Status,
            inviteRequest.CreatedAt,
            inviteRequest.RespondedAt
        );
    }
}
