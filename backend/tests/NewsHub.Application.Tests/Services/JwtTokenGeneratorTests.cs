namespace NewsHub.Application.Tests.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Infrastructure.Services;
using Xunit;

public class JwtTokenGeneratorTests
{
    private readonly JwtOptions _options = new()
    {
        SecretKey = "super-secret-key-that-is-at-least-32-characters-long!",
        Issuer = "NewsHubApiTest",
        Audience = "NewsHubAppTest",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    private readonly JwtTokenGenerator _generator;

    public JwtTokenGeneratorTests()
    {
        var optionsMock = Options.Create(_options);
        _generator = new JwtTokenGenerator(optionsMock);
    }

    [Fact]
    public void GenerateAccessToken_ShouldProduceValidJwtWithClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "testuser@newshub.com",
            PasswordHash = "hash",
            FullName = "Test User",
            Role = UserRole.Journalist
        };

        // Act
        var tokenString = _generator.GenerateAccessToken(user);

        // Assert
        Assert.NotNull(tokenString);
        Assert.NotEmpty(tokenString);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenString);

        Assert.Equal(_options.Issuer, jwt.Issuer);
        Assert.Contains(jwt.Audiences, a => a == _options.Audience);

        var subClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
        Assert.Equal(user.Id.ToString(), subClaim);

        var emailClaim = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
        Assert.Equal(user.Email, emailClaim);

        var roleClaim = jwt.Claims.FirstOrDefault(c => c.Type == "role" || c.Type == ClaimTypes.Role)?.Value;
        Assert.Equal("Journalist", roleClaim);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldProduceRawTokenAndMatchingHash()
    {
        // Act
        var (rawToken, tokenHash) = _generator.GenerateRefreshToken();

        // Assert
        Assert.NotNull(rawToken);
        Assert.NotEmpty(rawToken);
        Assert.Equal(128, rawToken.Length); // 64 bytes = 128 hex chars

        Assert.NotNull(tokenHash);
        Assert.NotEmpty(tokenHash);
        Assert.Equal(64, tokenHash.Length); // SHA-256 = 32 bytes = 64 hex chars

        var expectedHash = _generator.HashRefreshToken(rawToken);
        Assert.Equal(expectedHash, tokenHash);
    }

    [Fact]
    public void ExpirationProperties_ShouldMatchOptions()
    {
        Assert.Equal(15 * 60, _generator.AccessTokenExpirationSeconds);
        Assert.Equal(7, _generator.RefreshTokenExpirationDays);
    }
}
