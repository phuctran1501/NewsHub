namespace NewsHub.Application.Features.Companies.Commands.RespondToJoinRequest;

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

public sealed record RespondToJoinRequestCommand(Guid CompanyId, Guid RequestId, bool Approve) : IRequest, ITransactionalRequest;

public sealed class RespondToJoinRequestCommandValidator : AbstractValidator<RespondToJoinRequestCommand>
{
    public RespondToJoinRequestCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Định danh yêu cầu không được để trống.");
    }
}

public sealed class RespondToJoinRequestCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<RespondToJoinRequestCommandHandler> logger) : IRequestHandler<RespondToJoinRequestCommand>
{
    public async Task Handle(RespondToJoinRequestCommand request, CancellationToken cancellationToken)
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
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền duyệt đơn xin gia nhập.", ErrorCodes.OnlyOwnerAllowed);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        var joinRequest = await dbContext.CompanyRequests
            .Include(r => r.Journalist)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId && r.CompanyId == request.CompanyId && r.Type == CompanyRequestType.JoinRequest, cancellationToken);

        if (joinRequest == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy đơn xin gia nhập tương ứng.", ErrorCodes.RequestNotFound);
        }

        if (joinRequest.Status != CompanyRequestStatus.Pending)
        {
            throw new BadRequestException("Đơn xin gia nhập này đã được xử lý trước đó.", ErrorCodes.InvalidRequestStatus);
        }

        if (request.Approve)
        {
            // Kiểm tra giới hạn 5 công ty của người nộp đơn
            var activeCount = await dbContext.CompanyMembers
                .CountAsync(m => m.UserId == joinRequest.JournalistId, cancellationToken);

            if (activeCount >= 5)
            {
                throw new BadRequestException("Nhà báo này đã tham gia tối đa 5 tòa soạn. Không thể duyệt thêm.", ErrorCodes.MaxCompaniesLimitReached);
            }

            var isAlreadyMember = await dbContext.CompanyMembers
                .AnyAsync(m => m.CompanyId == request.CompanyId && m.UserId == joinRequest.JournalistId, cancellationToken);

            if (!isAlreadyMember)
            {
                var member = new CompanyMember
                {
                    CompanyId = request.CompanyId,
                    UserId = joinRequest.JournalistId,
                    Role = CompanyMemberRole.Member,
                    JoinedAt = DateTimeOffset.UtcNow
                };
                dbContext.CompanyMembers.Add(member);
            }

            joinRequest.Status = CompanyRequestStatus.Accepted;
            joinRequest.RespondedAt = DateTimeOffset.UtcNow;

            logger.LogInformation("Owner {OwnerId} đã duyệt đơn gia nhập của nhà báo {JournalistId} vào công ty {CompanyId}.",
                currentUserId, joinRequest.JournalistId, request.CompanyId);
        }
        else
        {
            joinRequest.Status = CompanyRequestStatus.Rejected;
            joinRequest.RespondedAt = DateTimeOffset.UtcNow;

            logger.LogInformation("Owner {OwnerId} đã từ chối đơn gia nhập của nhà báo {JournalistId} vào công ty {CompanyId}.",
                currentUserId, joinRequest.JournalistId, request.CompanyId);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        // Bắn SignalR tới nhà báo nộp đơn sau khi commit DB thành công
        if (request.Approve)
        {
            try
            {
                var notification = new NotificationDto(
                    Guid.CreateVersion7(),
                    NotificationType.CompanyJoinRequest,
                    "Đơn xin gia nhập đã được duyệt",
                    $"Chúc mừng! Đơn xin gia nhập tòa soạn {company.Name} của bạn đã được phê duyệt.",
                    company.Id
                );
                await notificationService.SendToUserAsync(joinRequest.JournalistId, notification, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Lỗi gửi SignalR notification tới nhà báo {JournalistId}.", joinRequest.JournalistId);
            }
        }
    }
}
