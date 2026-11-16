namespace NewsHub.Application.Features.Auth.Commands.Logout;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;

public sealed record LogoutCommand(string? RawRefreshToken) : IRequest<bool>, ITransactionalRequest;

public sealed class LogoutCommandHandler(
    IApplicationDbContext dbContext,
    IJwtTokenGenerator jwtTokenGenerator) : IRequestHandler<LogoutCommand, bool>
{
    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawRefreshToken))
        {
            return true;
        }

        var tokenHash = jwtTokenGenerator.HashRefreshToken(request.RawRefreshToken);

        var token = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (token != null && !token.IsRevoked && token.RevokedAt == null)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTimeOffset.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
