namespace NewsHub.Application.Tests.Features.Auth.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.Register;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RegisterCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly RegisterCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidReaderData_ShouldCreateUserAndReturnDto()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _passwordHasherMock.Setup(p => p.HashPassword("ValidPassword123!"))
            .Returns("hashed_password_xyz");

        var handler = new RegisterCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterCommand("reader@example.com", "ValidPassword123!", "John Reader");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("reader@example.com", result.Email);
        Assert.Equal("John Reader", result.FullName);
        Assert.Equal(UserRole.User, result.Role);

        _dbContextMock.Verify(db => db.Users.Add(
            It.Is<User>(u => u.Email.Value == "reader@example.com"
                && u.FullName == "John Reader"
                && u.Role == UserRole.User
                && u.PasswordHash == "hashed_password_xyz"
                && u.IsActive)), Times.Once);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmailAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var existingUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "existing@example.com",
            PasswordHash = "hashed",
            FullName = "Existing User",
            Role = UserRole.User,
            IsActive = true
        };
        var users = new List<User> { existingUser }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new RegisterCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterCommand("Existing@Example.com", "ValidPassword123!", "Existing User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.ErrorCode);
        _dbContextMock.Verify(db => db.Users.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenConcurrentInsertThrowsDbUpdateException_ShouldThrowConflictException()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Microsoft.EntityFrameworkCore.DbUpdateException("Duplicate key violation"));

        _passwordHasherMock.Setup(p => p.HashPassword(It.IsAny<string>()))
            .Returns("hashed");

        var handler = new RegisterCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterCommand("concurrent@example.com", "ValidPassword123!", "Concurrent User");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.ErrorCode);
    }

    [Theory]
    [InlineData("", "Valid123!@", "Name")] // Empty email
    [InlineData("not-an-email", "Valid123!@", "Name")] // Invalid format
    [InlineData("test@domain.com", "short", "Name")] // Short password
    [InlineData("test@domain.com", "alllowercase123", "Name")] // No uppercase
    [InlineData("test@domain.com", "ALLUPPERCASE123", "Name")] // No lowercase
    [InlineData("test@domain.com", "NoDigitsPassword", "Name")] // No digit
    [InlineData("test@domain.com", "Valid123!@", "")] // Empty full name
    [InlineData("test@domain.com", "Valid123!@", "A")] // Too short full name (<2)
    [InlineData("test@domain.com", "Valid123!@", " A ")] // Trimmed full name too short (<2)
    public void Validator_WithInvalidInputs_ShouldHaveValidationErrors(string email, string password, string fullName)
    {
        var command = new RegisterCommand(email, password, fullName);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_WithValidInputs_ShouldPass()
    {
        var command = new RegisterCommand("valid@domain.com", "Valid123!@#", "Valid User");
        var result = _validator.TestValidate(command);
        Assert.True(result.IsValid);
    }
}
