namespace NewsHub.Application.Features.Auth.Commands.Register;

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

public sealed record RegisterCommand(
    string Email,
    string Password,
    string FullName) : IRequest<AuthUserResponseDto>, ITransactionalRequest;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
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
    }
}

public sealed class RegisterCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher) : IRequestHandler<RegisterCommand, AuthUserResponseDto>
{
    public async Task<AuthUserResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
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

        var user = new User
        {
            Email = new Email(normalizedEmail),
            PasswordHash = passwordHash,
            FullName = request.FullName.Trim(),
            Role = UserRole.User,
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

        return new AuthUserResponseDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            user.CreatedAt);
    }
}
