namespace NewsHub.Application.Features.Admin.Commands.ToggleUserActiveStatus;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

/// <summary>
/// Command khóa hoặc mở khóa tài khoản người dùng, đồng thời thu hồi toàn bộ Refresh Token khi khóa tài khoản.
/// </summary>
public sealed record ToggleUserActiveStatusCommand(
    Guid UserId,
    bool IsActive,
    string? LockReason = null) : IRequest;

public sealed class ToggleUserActiveStatusCommandValidator : AbstractValidator<ToggleUserActiveStatusCommand>
{
    public ToggleUserActiveStatusCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Định danh người dùng (userId) không được để trống.");

        RuleFor(x => x.LockReason)
            .MaximumLength(500).WithMessage("Lý do khóa tài khoản không được vượt quá 500 ký tự.");
    }
}

public sealed class ToggleUserActiveStatusCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork) : IRequestHandler<ToggleUserActiveStatusCommand>
{
    public async Task Handle(ToggleUserActiveStatusCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền thay đổi trạng thái tài khoản người dùng.", ErrorCodes.PermissionDenied);
        }

        // Chống tự khóa tài khoản Admin
        if (request.UserId == currentUser.Id)
        {
            throw new BadRequestException("Bạn không thể tự khóa tài khoản quản trị của chính mình.", ErrorCodes.CannotLockOwnAccount);
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw NotFoundException.WithErrorCode("Không tìm thấy người dùng yêu cầu.", ErrorCodes.UserNotFound);
        }

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Nếu khóa tài khoản: thu hồi lập tức toàn bộ Refresh Token đang hoạt động để cưỡng bức đăng xuất khỏi mọi thiết bị
        if (!request.IsActive)
        {
            var activeTokens = await dbContext.RefreshTokens
                .Where(t => t.UserId == request.UserId && !t.IsRevoked)
                .ToListAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = now;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
