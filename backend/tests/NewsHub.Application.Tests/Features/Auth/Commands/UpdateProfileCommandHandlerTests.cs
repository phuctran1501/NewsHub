namespace NewsHub.Application.Tests.Features.Auth.Commands;

using FluentValidation.TestHelper;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Auth.Commands.UpdateProfile;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class UpdateProfileCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly UpdateProfileCommandValidator _validator = new();

    [Fact]
    public async Task Handle_WithValidReaderUpdate_ShouldUpdateAllowedFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var user = new User
        {
            Id = userId,
            Email = "reader@example.com",
            PasswordHash = "hash",
            FullName = "Old Name",
            AvatarUrl = "https://example.com/old.png",
            Role = UserRole.User,
            IsActive = true
        };

        var users = new List<User> { user }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var handler = new UpdateProfileCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
        var command = new UpdateProfileCommand(
            "New Reader Name",
            "https://example.com/new.png",
            "This is a reader bio",
            null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("New Reader Name", result.FullName);
        Assert.Equal("https://example.com/new.png", result.AvatarUrl);
        Assert.Equal("This is a reader bio", result.Bio);
        Assert.Equal("New Reader Name", user.FullName);
        Assert.Equal("https://example.com/new.png", user.AvatarUrl);
        Assert.Equal("This is a reader bio", user.Bio);
        Assert.NotNull(user.UpdatedAt);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidPhoneUpdate_ShouldUpdateAndNormalizePhoneNumber()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var journalist = new User
        {
            Id = userId,
            Email = "journalist@example.com",
            PasswordHash = "hash",
            FullName = "Reporter Name",
            Role = UserRole.Journalist,
            PhoneNumber = "+84900000001",
            PressCardNumber = "OLD-CARD-1",
            IsActive = true
        };

        var users = new List<User> { journalist }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _dbContextMock.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var handler = new UpdateProfileCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
        var command = new UpdateProfileCommand(
            "Updated Journalist",
            null,
            "Updated Bio",
            "0912345678",
            "NEW-CARD-99");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("+84912345678", result.PhoneNumber);
        Assert.Equal("+84912345678", journalist.PhoneNumber);
        Assert.Equal("NEW-CARD-99", result.PressCardNumber);
        Assert.Equal("NEW-CARD-99", journalist.PressCardNumber);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        _currentUserMock.Setup(c => c.Id).Returns((Guid?)null);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var handler = new UpdateProfileCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
        var command = new UpdateProfileCommand("Name", null, null, null);

        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowNotFoundException()
    {
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var users = new List<User>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);

        var handler = new UpdateProfileCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
        var command = new UpdateProfileCommand("Name", null, null, null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(command, CancellationToken.None));
        Assert.Equal(ErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public void Validator_WhenAvatarUrlInvalid_ShouldFail()
    {
        var command = new UpdateProfileCommand("Name", "not-a-valid-url", null, null);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.AvatarUrl));
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("091234567")]
    [InlineData("not-a-phone")]
    public void Validator_WhenPhoneNumberInvalid_ShouldFail(string invalidPhone)
    {
        var command = new UpdateProfileCommand("Name", null, null, invalidPhone);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.PhoneNumber));
    }

    [Fact]
    public void Validator_WhenPhoneNumberValid_ShouldPass()
    {
        var command = new UpdateProfileCommand("Name", null, null, "0987654321");
        var result = _validator.TestValidate(command);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_WhenFullNameTrimmedTooShort_ShouldFail()
    {
        var command = new UpdateProfileCommand(" A ", null, null, null);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.FullName));
    }

    [Fact]
    public void Validator_WhenPressCardNumberExceeds50_ShouldFail()
    {
        var longCardNumber = new string('A', 51);
        var command = new UpdateProfileCommand("Name", null, null, null, longCardNumber);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.PressCardNumber));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_WhenPressCardNumberWhitespace_ShouldFail(string emptyCard)
    {
        var command = new UpdateProfileCommand("Name", null, null, null, emptyCard);
        var result = _validator.TestValidate(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(UpdateProfileCommand.PressCardNumber));
    }

    [Fact]
    public async Task Handle_WhenReaderAttemptsToSetPressCardNumber_ShouldThrowBadRequestException()
    {
        var userId = Guid.NewGuid();
        var reader = new User
        {
            Id = userId,
            Email = "reader@example.com",
            PasswordHash = "hash",
            FullName = "Reader",
            Role = UserRole.User,
            IsActive = true
        };

        var users = new List<User> { reader }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.Users).Returns(users.Object);
        _currentUserMock.Setup(c => c.Id).Returns(userId);
        _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);

        var handler = new UpdateProfileCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
        var command = new UpdateProfileCommand(null, null, null, null, "NB-FAKE-01");

        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(command, CancellationToken.None));
    }
}
