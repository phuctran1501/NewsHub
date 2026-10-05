using NewsHub.Domain.Entities;
using Xunit;

namespace NewsHub.Domain.Tests.Entities;

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_WhenNotRevokedAndNotExpired_ShouldBeActive()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "sha256-hash-value",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = false
        };

        // Assert
        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void RefreshToken_WhenRevoked_ShouldNotBeActive()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "sha256-hash-value",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
            IsRevoked = true,
            RevokedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.False(token.IsActive);
        Assert.NotNull(token.RevokedAt);
    }

    [Fact]
    public void RefreshToken_WhenExpired_ShouldNotBeActive()
    {
        // Arrange
        var token = new RefreshToken
        {
            UserId = Guid.NewGuid(),
            TokenHash = "sha256-hash-value",
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(-10),
            IsRevoked = false
        };

        // Assert
        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }
}
