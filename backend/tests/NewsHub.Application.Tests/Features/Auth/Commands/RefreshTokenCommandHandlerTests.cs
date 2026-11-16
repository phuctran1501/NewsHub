namespace NewsHub.Application.Tests.Features.Auth.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.RefreshToken;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RefreshTokenCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();

    public RefreshTokenCommandHandlerTests()
    {
        _jwtTokenGeneratorMock.Setup(j => j.AccessTokenExpirationSeconds).Returns(900);
        _jwtTokenGeneratorMock.Setup(j => j.RefreshTokenExpirationDays).Returns(7);
        _jwtTokenGeneratorMock.Setup(j => j.HashRefreshToken("raw_token_123")).Returns("hash_123");
        _jwtTokenGeneratorMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("new_access_token");
        _jwtTokenGeneratorMock.Setup(j => j.GenerateRefreshToken()).Returns(("new_raw_token", "new_hash_token"));
    }

    [Fact]
    public async Task Handle_WithValidToken_ShouldRotateTokenAndReturnResult()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "hashed_pwd",
            FullName = "User",
            Role = UserRole.User,
            IsActive = true
        };

        var existingToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_123",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(3),
            IsRevoked = false,
            RevokedAt = null,
            User = user
        };

        var tokens = new List<RefreshToken> { existingToken }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("new_access_token", result.AccessToken);
        Assert.Equal("new_raw_token", result.RawRefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(900, result.ExpiresIn);

        // Verify existing token was revoked and replaced
        Assert.True(existingToken.IsRevoked);
        Assert.NotNull(existingToken.RevokedAt);
        Assert.Equal("new_hash_token", existingToken.ReplacedByTokenHash);

        // Verify new token was added
        _dbContextMock.Verify(db => db.RefreshTokens.Add(It.Is<RefreshToken>(rt =>
            rt.UserId == userId
            && rt.TokenHash == "new_hash_token"
            && !rt.IsRevoked)), Times.Once);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenTokenIsEmptyOrNull_ShouldThrowUnauthorizedException(string? token)
    {
        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand(token);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.RefreshTokenRequired, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ShouldThrowUnauthorizedException()
    {
        var tokens = new List<RefreshToken>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ShouldTriggerTokenReuseDetectionAndRevokeAllSessions()
    {
        // Arrange: Token reuse scenario
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "reused@example.com",
            PasswordHash = "hash",
            FullName = "Reused User",
            IsActive = true
        };

        var reusedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_123",
            IsRevoked = true,
            RevokedAt = DateTimeOffset.UtcNow.AddHours(-1),
            User = user
        };

        var otherActiveToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "other_active_hash",
            IsRevoked = false,
            RevokedAt = null,
            User = user
        };

        var tokens = new List<RefreshToken> { reusedToken, otherActiveToken }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.TokenReuseDetected, ex.ErrorCode);

        // Verify otherActiveToken is now revoked
        Assert.True(otherActiveToken.IsRevoked);
        Assert.NotNull(otherActiveToken.RevokedAt);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiredToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_123",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-5),
            IsRevoked = false,
            User = new User
            {
                Id = userId,
                Email = "expired@example.com",
                PasswordHash = "hash",
                FullName = "Expired",
                IsActive = true
            }
        };

        var tokens = new List<RefreshToken> { expiredToken }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.RefreshTokenExpired, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var validToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_123",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            IsRevoked = false,
            User = new User
            {
                Id = userId,
                Email = "inactive@example.com",
                PasswordHash = "hash",
                FullName = "Inactive",
                IsActive = false
            }
        };

        var tokens = new List<RefreshToken> { validToken }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidCredentials, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenDbUpdateConcurrencyExceptionOccurs_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var validToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = "hash_123",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(1),
            IsRevoked = false,
            User = new User
            {
                Id = userId,
                Email = "active@example.com",
                PasswordHash = "hash",
                FullName = "Active",
                IsActive = true
            }
        };

        var tokens = new List<RefreshToken> { validToken }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(tokens.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException("Concurrency conflict"));

        var handler = new RefreshTokenCommandHandler(_dbContextMock.Object, _jwtTokenGeneratorMock.Object);
        var command = new RefreshTokenCommand("raw_token_123");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidRefreshToken, ex.ErrorCode);
    }
}
