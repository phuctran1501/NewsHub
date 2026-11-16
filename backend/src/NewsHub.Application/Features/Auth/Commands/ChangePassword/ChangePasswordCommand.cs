namespace NewsHub.Application.Features.Auth.Commands.ChangePassword;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;

public sealed record ChangePasswordCommand(
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<bool>, ITransactionalRequest;

public sealed class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mật khẩu hiện tại không được để trống.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Mật khẩu mới không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu mới phải có ít nhất 8 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu mới không được vượt quá 100 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu mới phải chứa ít nhất 1 chữ số.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Mật khẩu mới không được trùng với mật khẩu hiện tại.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Xác nhận mật khẩu không được để trống.")
            .Equal(x => x.NewPassword).WithMessage("Xác nhận mật khẩu không khớp với mật khẩu mới.");
    }
}

public sealed class ChangePasswordCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IPasswordHasher passwordHasher) : IRequestHandler<ChangePasswordCommand, bool>
{
    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Id == null)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để thực hiện thao tác này.", ErrorCodes.Unauthorized);
        }

        var userId = currentUser.Id.Value;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw NotFoundException.WithErrorCode("Người dùng không tồn tại hoặc đã bị khóa.", ErrorCodes.UserNotFound);
        }

        var isCurrentPasswordValid = passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            throw new BadRequestException("Mật khẩu hiện tại không chính xác.", ErrorCodes.AuthInvalidCurrentPassword);
        }

        user.PasswordHash = passwordHasher.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTimeOffset.UtcNow;

        // Lưu ý: Không thu hồi active refresh tokens trên các thiết bị khác (Session Retention per specification)
        await dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}
