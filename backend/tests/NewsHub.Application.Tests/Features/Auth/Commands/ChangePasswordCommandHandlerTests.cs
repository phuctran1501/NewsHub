namespace NewsHub.Application.Tests.Features.Auth.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.ChangePassword;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly ChangePasswordCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidPasswords_ShouldUpdatePasswordHashAndRetainSessions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "old_hashed_password",
            FullName = "User",
            Role = UserRole.User,
            IsActive = true
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        _passwordHasherMock.Setup(p => p.VerifyPassword("CurrentPassword123!", "old_hashed_password"))
            .Returns(true);
        _passwordHasherMock.Setup(p => p.HashPassword("NewStrongPassword456!"))
            .Returns("new_hashed_password");

        var handler = new ChangePasswordCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _passwordHasherMock.Object);

        var command = new ChangePasswordCommand("CurrentPassword123!", "NewStrongPassword456!", "NewStrongPassword456!");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        Assert.Equal("new_hashed_password", user.PasswordHash);
        Assert.NotNull(user.UpdatedAt);

        // Crucial requirement: No tokens were revoked or manipulated
        _dbContextMock.Verify(db => db.RefreshTokens, Times.Never);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = new ChangePasswordCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _passwordHasherMock.Object);

        var command = new ChangePasswordCommand("CurrentPassword123!", "NewStrongPassword456!", "NewStrongPassword456!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserNotFoundInDatabase_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new ChangePasswordCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _passwordHasherMock.Object);

        var command = new ChangePasswordCommand("CurrentPassword123!", "NewStrongPassword456!", "NewStrongPassword456!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCurrentPasswordIncorrect_ShouldThrowBadRequestException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "user@example.com",
            PasswordHash = "actual_hashed_password",
            FullName = "User",
            IsActive = true
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        _passwordHasherMock.Setup(p => p.VerifyPassword("WrongCurrentPassword", "actual_hashed_password"))
            .Returns(false);

        var handler = new ChangePasswordCommandHandler(
            _dbContextMock.Object,
            _currentUserMock.Object,
            _passwordHasherMock.Object);

        var command = new ChangePasswordCommand("WrongCurrentPassword", "NewStrongPassword456!", "NewStrongPassword456!");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.AuthInvalidCurrentPassword, ex.ErrorCode);
    }

    [Theory]
    [InlineData("", "ValidPassword123!", "ValidPassword123!")] // empty current
    [InlineData("Current123!", "", "")] // empty new
    [InlineData("Current123!", "short", "short")] // short new (<8)
    [InlineData("Current123!", "nocapsdigits123", "nocapsdigits123")] // no uppercase
    [InlineData("Current123!", "NOLOWERCAS123", "NOLOWERCAS123")] // no lowercase
    [InlineData("Current123!", "NoDigitsPassword", "NoDigitsPassword")] // no digits
    [InlineData("Current123!", "Current123!", "Current123!")] // same as current
    [InlineData("Current123!", "Valid123!@#", "Different123!@#")] // mismatch confirm
    public void Validator_WithInvalidInputs_ShouldFail(string currentPassword, string newPassword, string confirmPassword)
    {
        var command = new ChangePasswordCommand(currentPassword, newPassword, confirmPassword);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }
}
