namespace NewsHub.Application.Common.Interfaces;

using NewsHub.Domain.Entities;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(User user);
    (string RawToken, string TokenHash) GenerateRefreshToken();
    string HashRefreshToken(string rawToken);
    int AccessTokenExpirationSeconds { get; }
    int RefreshTokenExpirationDays { get; }
}
