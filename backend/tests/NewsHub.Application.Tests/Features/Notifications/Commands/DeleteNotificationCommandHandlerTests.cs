namespace NewsHub.Application.Tests.Features.Notifications.Commands;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Notifications.Commands.DeleteNotification;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class DeleteNotificationCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private DeleteNotificationCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new DeleteNotificationCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNotificationNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        _dbContextMock.Setup(db => db.Notifications).Returns(new List<Notification>().BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteNotificationCommand(Guid.NewGuid()), CancellationToken.None));
        Assert.Equal(ErrorCodes.NotificationNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNotificationBelongsToAnotherUser_ShouldThrowNotFoundException()
    {
        // Arrange (Chống IDOR)
        var userId = Guid.NewGuid();
        var anotherUserId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var notifications = new List<Notification>
        {
            new()
            {
                Id = notificationId,
                UserId = anotherUserId,
                Type = NotificationType.SecurityAlert,
                Title = "Bảo mật",
                Message = "Cảnh báo",
                IsRead = false
            }
        };

        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new DeleteNotificationCommand(notificationId), CancellationToken.None));
        Assert.Equal(ErrorCodes.NotificationNotFound, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNotificationExists_ShouldRemoveAndSave()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.NewGuid();

        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var notification = new Notification
        {
            Id = notificationId,
            UserId = userId,
            Type = NotificationType.CompanyInvitation,
            Title = "Lời mời",
            Message = "Gia nhập",
            IsRead = false
        };

        var notifications = new List<Notification> { notification };
        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act
        await handler.Handle(new DeleteNotificationCommand(notificationId), CancellationToken.None);

        // Assert
        _dbContextMock.Verify(db => db.Notifications.Remove(notification), Times.Once);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
