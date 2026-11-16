namespace NewsHub.Application.Features.Auth.Commands.RefreshToken;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Exceptions;

public sealed record RefreshTokenCommand(string? RawRefreshToken) : IRequest<RefreshTokenResultDto>, ITransactionalRequest;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<RefreshTokenCommand, RefreshTokenResultDto>
{
    public async Task<RefreshTokenResultDto> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            throw new UnauthorizedException("Refresh token is required.", ErrorCodes.RefreshTokenRequired);
        }

        var tokenHash = jwtTokenGenerator.HashRefreshToken(request.RawRefreshToken);

        var token = await dbContext.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (token == null)
        {
            throw new UnauthorizedException("Phiên làm việc đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.", ErrorCodes.InvalidRefreshToken);
        }

        // Token Reuse Detection
        if (token.IsRevoked || token.RevokedAt != null)
        {
            var activeTokens = await dbContext.RefreshTokens
                .Where(r => r.UserId == token.UserId && !r.IsRevoked && r.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var activeToken in activeTokens)
            {
                activeToken.IsRevoked = true;
                activeToken.RevokedAt = DateTimeOffset.UtcNow;
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            throw new UnauthorizedException("Dấu hiệu chiếm đoạt phiên đăng nhập được phát hiện. Mọi phiên hoạt động đã bị thu hồi.", ErrorCodes.TokenReuseDetected);
        }

        // Token Expiration Check
        if (token.IsExpired || token.ExpiresAt < DateTimeOffset.UtcNow)
        {
            throw new UnauthorizedException("Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.", ErrorCodes.RefreshTokenExpired);
        }

        // User Active Check
        if (token.User == null || !token.User.IsActive)
        {
            throw new UnauthorizedException("Tài khoản không hoạt động hoặc không tồn tại.", ErrorCodes.InvalidCredentials);
        }

        // Token Rotation
        var newAccessToken = jwtTokenGenerator.GenerateAccessToken(token.User);
        var (newRawRefreshToken, newTokenHash) = jwtTokenGenerator.GenerateRefreshToken();

        token.IsRevoked = true;
        token.RevokedAt = DateTimeOffset.UtcNow;
        token.ReplacedByTokenHash = newTokenHash;

        var newRefreshToken = new RefreshToken
        {
            UserId = token.UserId,
            TokenHash = newTokenHash,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(jwtTokenGenerator.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.RefreshTokens.Add(newRefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResultDto(
            newAccessToken,
            newRawRefreshToken,
            "Bearer",
            jwtTokenGenerator.AccessTokenExpirationSeconds);
    }
}
