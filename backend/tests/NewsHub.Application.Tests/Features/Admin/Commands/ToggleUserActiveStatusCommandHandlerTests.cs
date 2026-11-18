namespace NewsHub.Application.Tests.Features.Admin.Commands;

using MockQueryable;
using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Commands.ToggleUserActiveStatus;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using NewsHub.Domain.ValueObjects;
using Xunit;

public class ToggleUserActiveStatusCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();

    private ToggleUserActiveStatusCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object, _unitOfWorkMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new ToggleUserActiveStatusCommand(Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUserIsNotAdmin_ShouldThrowForbiddenException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new ToggleUserActiveStatusCommand(Guid.NewGuid(), false), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAdminAttemptsToLockOwnAccount_ShouldThrowBadRequestException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            handler.Handle(new ToggleUserActiveStatusCommand(adminId, false), CancellationToken.None));
        Assert.Equal(ErrorCodes.CannotLockOwnAccount, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenTargetUserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        _dbContextMock.Setup(db => db.Users).Returns(new List<User>().BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ToggleUserActiveStatusCommand(targetUserId, false), CancellationToken.None));
        Assert.Equal(ErrorCodes.UserNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenLockingUser_ShouldSetIsActiveFalseAndRevokeActiveRefreshTokens()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        var targetUser = new User
        {
            Id = targetUserId,
            Email = new Email("user@example.com"),
            FullName = "Người Dùng Bị Khóa",
            PasswordHash = "hash",
            IsActive = true
        };

        var activeToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            TokenHash = "hash1",
            IsRevoked = false,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var alreadyRevokedToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            TokenHash = "hash2",
            IsRevoked = true,
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        _dbContextMock.Setup(db => db.Users).Returns(new List<User> { targetUser }.BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.RefreshTokens).Returns(new List<RefreshToken> { activeToken, alreadyRevokedToken }.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new ToggleUserActiveStatusCommand(targetUserId, false, "Vi phạm tiêu chuẩn"), CancellationToken.None);

        // Assert
        Assert.False(targetUser.IsActive);
        Assert.NotNull(targetUser.UpdatedAt);
        Assert.True(activeToken.IsRevoked);
        Assert.NotNull(activeToken.RevokedAt);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnlockingUser_ShouldSetIsActiveTrue()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var targetUserId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(adminId);

        var targetUser = new User
        {
            Id = targetUserId,
            Email = new Email("user@example.com"),
            FullName = "Người Dùng Mở Khóa",
            PasswordHash = "hash",
            IsActive = false
        };

        _dbContextMock.Setup(db => db.Users).Returns(new List<User> { targetUser }.BuildMockDbSet().Object);
        var handler = CreateHandler();

        // Act
        await handler.Handle(new ToggleUserActiveStatusCommand(targetUserId, true), CancellationToken.None);

        // Assert
        Assert.True(targetUser.IsActive);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
