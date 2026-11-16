namespace NewsHub.Application.Features.Auth.Commands.UpdateProfile;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;

public sealed record UpdateProfileCommand(
    string? FullName,
    string? AvatarUrl,
    string? Bio,
    string? PhoneNumber,
    string? PressCardNumber = null) : IRequest<UserProfileResponseDto>, ITransactionalRequest;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        When(x => x.FullName != null, () =>
        {
            RuleFor(x => x.FullName!)
                .NotEmpty().WithMessage("Họ tên không được để trống khi cập nhật.")
                .Must(name => name.Trim().Length >= 2 && name.Trim().Length <= 100)
                .WithMessage("Họ tên phải từ 2 đến 100 ký tự.");
        });

        When(x => !string.IsNullOrEmpty(x.AvatarUrl), () =>
        {
            RuleFor(x => x.AvatarUrl!)
                .MaximumLength(1024).WithMessage("Đường dẫn ảnh đại diện không được vượt quá 1024 ký tự.")
                .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsed) &&
                             (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Đường dẫn ảnh đại diện phải là URL hợp lệ (http hoặc https).");
        });

        When(x => x.Bio != null, () =>
        {
            RuleFor(x => x.Bio!)
                .MaximumLength(500).WithMessage("Tiểu sử không được vượt quá 500 ký tự.");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Must(PhoneNumber.IsValid)
                .WithMessage("Số điện thoại không đúng định dạng nhà mạng Việt Nam (+84).");
        });

        When(x => !string.IsNullOrWhiteSpace(x.PressCardNumber), () =>
        {
            RuleFor(x => x.PressCardNumber!)
                .MaximumLength(50).WithMessage("Số thẻ nhà báo không được vượt quá 50 ký tự.");
        });
    }
}

public sealed class UpdateProfileCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUser currentUser) : IRequestHandler<UpdateProfileCommand, UserProfileResponseDto>
{
    public async Task<UserProfileResponseDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
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

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            user.FullName = request.FullName.Trim();
        }

        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        }

        if (request.Bio != null)
        {
            user.Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim();
        }

        if (request.PhoneNumber != null)
        {
            user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
                ? null
                : PhoneNumber.Normalize(request.PhoneNumber);
        }

        if (request.PressCardNumber != null)
        {
            user.PressCardNumber = string.IsNullOrWhiteSpace(request.PressCardNumber)
                ? null
                : request.PressCardNumber.Trim();
        }

        user.UpdatedAt = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new UserProfileResponseDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.PhoneNumber,
            user.PressCardNumber,
            user.Bio,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);
    }
}
