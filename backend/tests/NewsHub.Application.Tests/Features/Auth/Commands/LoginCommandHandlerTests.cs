namespace NewsHub.Application.Tests.Features.Auth.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.Login;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class LoginCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtTokenGenerator> _jwtTokenGeneratorMock = new();

    public LoginCommandHandlerTests()
    {
        _jwtTokenGeneratorMock.Setup(j => j.AccessTokenExpirationSeconds).Returns(900);
        _jwtTokenGeneratorMock.Setup(j => j.RefreshTokenExpirationDays).Returns(7);
        _jwtTokenGeneratorMock.Setup(j => j.GenerateAccessToken(It.IsAny<User>())).Returns("access_token_jwt");
        _jwtTokenGeneratorMock.Setup(j => j.GenerateRefreshToken()).Returns(("raw_refresh_token", "hash_refresh_token"));
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnLoginResultAndSaveRefreshToken()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = "stored_hashed_pwd",
            FullName = "John Doe",
            Role = UserRole.User,
            IsActive = true
        };
        var users = new List<User> { user }.BuildMockDbSet();
        var refreshTokens = new List<RefreshToken>().BuildMockDbSet();

        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(refreshTokens.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _passwordHasherMock.Setup(p => p.VerifyPassword("Password123!", "stored_hashed_pwd"))
            .Returns(true);

        var handler = new LoginCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);

        var command = new LoginCommand("User@Example.com", "Password123!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("access_token_jwt", result.AccessToken);
        Assert.Equal("raw_refresh_token", result.RawRefreshToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(900, result.ExpiresIn);
        Assert.Equal("user@example.com", result.User.Email);
        Assert.Equal(UserRole.User, result.User.Role);

        _dbContextMock.Verify(db => db.RefreshTokens.Add(It.Is<RefreshToken>(rt =>
            rt.UserId == user.Id
            && rt.TokenHash == "hash_refresh_token"
            && !rt.IsRevoked)), Times.Once);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new LoginCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);

        var command = new LoginCommand("notfound@example.com", "Password123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidCredentials, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenPasswordIncorrect_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = "stored_hashed_pwd",
            FullName = "John Doe",
            IsActive = true
        };
        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongPassword", "stored_hashed_pwd"))
            .Returns(false);

        var handler = new LoginCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);

        var command = new LoginCommand("user@example.com", "WrongPassword");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidCredentials, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "inactive@example.com",
            PasswordHash = "stored_hashed_pwd",
            FullName = "Inactive User",
            IsActive = false
        };
        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        _passwordHasherMock.Setup(p => p.VerifyPassword("Password123!", "stored_hashed_pwd"))
            .Returns(true);

        var handler = new LoginCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtTokenGeneratorMock.Object);

        var command = new LoginCommand("inactive@example.com", "Password123!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.InvalidCredentials, ex.ErrorCode);
    }
}
