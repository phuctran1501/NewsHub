namespace NewsHub.Application.Tests.Features.Auth.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.RegisterJournalist;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class RegisterJournalistCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly RegisterJournalistCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidJournalistData_ShouldCreateJournalistAndReturnDto()
    {
        // Arrange
        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _passwordHasherMock.Setup(p => p.HashPassword("ValidPassword123!"))
            .Returns("hashed_journalist_pw");

        var handler = new RegisterJournalistCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterJournalistCommand(
            "journalist@news.com",
            "ValidPassword123!",
            "Jane Journalist",
            "0912345678",
            "PRESS-2026-99",
            "Experienced investigative reporter.");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("journalist@news.com", result.Email);
        Assert.Equal("Jane Journalist", result.FullName);
        Assert.Equal(UserRole.Journalist, result.Role);
        Assert.Equal("+84912345678", result.PhoneNumber);
        Assert.Equal("PRESS-2026-99", result.PressCardNumber);
        Assert.Equal("Experienced investigative reporter.", result.Bio);

        _dbContextMock.Verify(db => db.Users.Add(
            It.Is<User>(u => u.Email.Value == "journalist@news.com"
                && u.FullName == "Jane Journalist"
                && u.Role == UserRole.Journalist
                && u.PhoneNumber == "+84912345678"
                && u.PressCardNumber == "PRESS-2026-99"
                && u.Bio == "Experienced investigative reporter."
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
            Email = "journalist@news.com",
            PasswordHash = "hash",
            FullName = "Jane",
            Role = UserRole.Journalist,
            IsActive = true
        };
        var users = new List<User> { existingUser }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new RegisterJournalistCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterJournalistCommand(
            "JOURNALIST@news.com",
            "ValidPassword123!",
            "Jane",
            "0912345678",
            "PRESS-2026-99",
            null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.ErrorCode);
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

        var handler = new RegisterJournalistCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
        var command = new RegisterJournalistCommand("concurrent.journalist@news.com", "ValidPassword123!", "Jane", null, "PRESS-01", null);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.EmailAlreadyExists, ex.ErrorCode);
    }

    [Theory]
    [InlineData("0123456789")] // Invalid prefix 01
    [InlineData("091234567")] // Too short
    [InlineData("09123456789")] // Too long
    [InlineData("invalid-phone")]
    public void Validator_WhenPhoneNumberIsInvalid_ShouldFail(string invalidPhone)
    {
        var command = new RegisterJournalistCommand("j@news.com", "Valid123!@", "Jane", invalidPhone, "PRESS-01", null);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterJournalistCommand.PhoneNumber));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WhenPressCardNumberIsEmpty_ShouldFail(string? emptyCard)
    {
        var command = new RegisterJournalistCommand("j@news.com", "Valid123!@", "Jane", "0987654321", emptyCard, null);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterJournalistCommand.PressCardNumber));
    }

    [Fact]
    public void Validator_WhenPressCardNumberExceeds50_ShouldFail()
    {
        var longCardNumber = new string('A', 51);
        var command = new RegisterJournalistCommand("j@news.com", "Valid123!@", "Jane", "0987654321", longCardNumber, null);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterJournalistCommand.PressCardNumber));
    }

    [Fact]
    public void Validator_WhenValid_ShouldPass()
    {
        var command = new RegisterJournalistCommand("j@news.com", "Valid123!@", "Jane", "0987654321", "PRESS-01", "Bio");
        var result = _validator.TestValidate(command);
        Assert.True(result.IsValid);
    }
}
