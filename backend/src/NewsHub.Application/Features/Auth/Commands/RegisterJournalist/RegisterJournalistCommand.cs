namespace NewsHub.Application.Features.Auth.Commands.RegisterJournalist;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;

public sealed record RegisterJournalistCommand(
    string Email,
    string Password,
    string FullName,
    string? PhoneNumber,
    string? PressCardNumber,
    string? Bio) : IRequest<JournalistResponseDto>, ITransactionalRequest;

public sealed class RegisterJournalistCommandValidator : AbstractValidator<RegisterJournalistCommand>
{
    public RegisterJournalistCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.")
            .MaximumLength(256).WithMessage("Email không được vượt quá 256 ký tự.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.")
            .MinimumLength(8).WithMessage("Mật khẩu phải có ít nhất 8 ký tự.")
            .MaximumLength(100).WithMessage("Mật khẩu không được vượt quá 100 ký tự.")
            .Matches(@"[A-Z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ hoa.")
            .Matches(@"[a-z]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ thường.")
            .Matches(@"[0-9]").WithMessage("Mật khẩu phải chứa ít nhất 1 chữ số.");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Họ tên không được để trống.")
            .Must(name => name.Trim().Length >= 2 && name.Trim().Length <= 100)
            .WithMessage("Họ tên phải từ 2 đến 100 ký tự.");

        When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber), () =>
        {
            RuleFor(x => x.PhoneNumber!)
                .Must(PhoneNumber.IsValid)
                .WithMessage("Số điện thoại không đúng định dạng nhà mạng Việt Nam (+84).");
        });

        RuleFor(x => x.PressCardNumber)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Số thẻ nhà báo không được để trống.")
            .Must(card => !string.IsNullOrWhiteSpace(card))
            .WithMessage("Số thẻ nhà báo không được để trống.")
            .Must(card => card!.Trim().Length <= 50)
            .WithMessage("Số thẻ nhà báo không được vượt quá 50 ký tự.");

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage("Tiểu sử không được vượt quá 500 ký tự.");
    }
}

public sealed class RegisterJournalistCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterJournalistCommand, JournalistResponseDto>
{
    public async Task<JournalistResponseDto> Handle(RegisterJournalistCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailVo = new Email(normalizedEmail);

        var emailExists = await dbContext.Users
            .AnyAsync(u => u.Email == emailVo, cancellationToken);

        if (emailExists)
        {
            throw new ConflictException("Email đã tồn tại trên hệ thống.", ErrorCodes.EmailAlreadyExists);
        }

        var passwordHash = passwordHasher.HashPassword(request.Password);

        // Lưu ý: Nhà báo đăng ký thành công ngay lập tức (IsActive = true), không cần đợi Admin duyệt
        var user = new User
        {
            Email = new Email(normalizedEmail),
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Role = UserRole.Journalist,
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : PhoneNumber.Normalize(request.PhoneNumber),
            PressCardNumber = string.IsNullOrWhiteSpace(request.PressCardNumber) ? null : request.PressCardNumber.Trim(),
            Bio = string.IsNullOrWhiteSpace(request.Bio) ? null : request.Bio.Trim(),
            IsActive = true
        };

        dbContext.Users.Add(user);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            throw new ConflictException("Email đã tồn tại trên hệ thống.", ErrorCodes.EmailAlreadyExists);
        }

        return new JournalistResponseDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            user.PhoneNumber,
            user.PressCardNumber,
            user.Bio,
            user.CreatedAt);
    }
}
