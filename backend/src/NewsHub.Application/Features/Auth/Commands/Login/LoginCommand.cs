namespace NewsHub.Application.Features.Auth.Commands.Login;

using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;

public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResultDto>, ITransactionalRequest;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email không được để trống.")
            .EmailAddress().WithMessage("Email không đúng định dạng.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Mật khẩu không được để trống.");
    }
}

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LoginCommand, LoginResultDto>
{
    public async Task<LoginResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var emailVo = new Email(normalizedEmail);

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == emailVo, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.", ErrorCodes.InvalidCredentials);
        }

        var isPasswordValid = passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedException("Email hoặc mật khẩu không chính xác.", ErrorCodes.InvalidCredentials);
        }

        var accessToken = jwtTokenGenerator.GenerateAccessToken(user);
        var (rawRefreshToken, tokenHash) = jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtTokenGenerator.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userProfile = new UserProfileResponseDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.AvatarUrl,
            user.Role,
            user.PhoneNumber,
            user.PhoneNumber,
            user.PressCardNumber,
            user.Bio,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt);

        return new LoginResultDto(
            accessToken,
            rawRefreshToken,
            "Bearer",
            jwtTokenGenerator.AccessTokenExpirationSeconds,
            userProfile);
    }
}
