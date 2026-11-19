namespace NewsHub.Application.Features.Companies.Commands.TransferOwnership;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

public sealed record TransferOwnershipCommand(Guid CompanyId, Guid NewOwnerId) : IRequest;

public sealed class TransferOwnershipCommandValidator : AbstractValidator<TransferOwnershipCommand>
{
    public TransferOwnershipCommandValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty().WithMessage("Định danh công ty không được để trống.");

        RuleFor(x => x.NewOwnerId)
            .NotEmpty().WithMessage("Định danh chủ sở hữu mới không được để trống.");
    }
}

public sealed class TransferOwnershipCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork,
    INotificationService notificationService,
    ILogger<TransferOwnershipCommandHandler> logger) : IRequestHandler<TransferOwnershipCommand>
{
    public async Task Handle(TransferOwnershipCommand request, CancellationToken cancellationToken)
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

        var currentOwnerMember = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == currentUserId && m.Role == CompanyMemberRole.Owner, cancellationToken);

        if (currentOwnerMember == null)
        {
            throw new ForbiddenException("Chỉ chủ sở hữu tòa soạn (Owner) mới có quyền chuyển giao quyền sở hữu.", ErrorCodes.OnlyOwnerAllowed);
        }

        if (request.NewOwnerId == currentUserId)
        {
            throw new BadRequestException("Bạn đang là chủ sở hữu của tòa soạn này.", ErrorCodes.InvalidRequest);
        }

        var newOwnerMember = await dbContext.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == request.CompanyId && m.UserId == request.NewOwnerId, cancellationToken);

        if (newOwnerMember == null)
        {
            throw new BadRequestException("Người tiếp quản phải đang là thành viên của tòa soạn.", ErrorCodes.NotCompanyMember);
        }

        var company = await dbContext.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);

        if (company == null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy tòa soạn tương ứng.", ErrorCodes.CompanyNotFound);
        }

        // Hoán đổi vai trò
        company.OwnerId = request.NewOwnerId;
        company.UpdatedAt = DateTimeOffset.UtcNow;

        currentOwnerMember.Role = CompanyMemberRole.Member;
        newOwnerMember.Role = CompanyMemberRole.Owner;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Owner cũ {CurrentUserId} đã chuyển quyền sở hữu công ty {CompanyId} cho {NewOwnerId}.",
            currentUserId, request.CompanyId, request.NewOwnerId);

        // Bắn SignalR notification tới Owner mới
        var notification = new NotificationDto(
            Guid.CreateVersion7(),
            NotificationType.CompanyMembershipChanged,
            "Tiếp quản quyền sở hữu Tòa soạn",
            $"Bạn đã chính thức trở thành Chủ sở hữu (Owner) của tòa soạn {company.Name}.",
            company.Id
        );
        await notificationService.SendToUserAsync(request.NewOwnerId, notification, cancellationToken);
    }
}
