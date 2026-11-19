namespace NewsHub.Application.Tests.Features.Admin.Commands;

using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Commands.SendSystemBroadcast;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class SendSystemBroadcastCommandHandlerTests
{
    private readonly Mock<INotificationService> _notificationServiceMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private SendSystemBroadcastCommandHandler CreateHandler() =>
        new(_notificationServiceMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new SendSystemBroadcastCommand("Tiêu đề", "Nội dung"), CancellationToken.None));
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
            handler.Handle(new SendSystemBroadcastCommand("Tiêu đề", "Nội dung"), CancellationToken.None));
        Assert.Equal(ErrorCodes.PermissionDenied, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenAdminBroadcasts_ShouldCallNotificationServiceBroadcastAsync()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.IsInRole(UserRole.Admin)).Returns(true);
        var handler = CreateHandler();

        var title = "Bảo trì khẩn cấp";
        var message = "Hệ thống sẽ bảo trì trong 10 phút.";

        // Act
        await handler.Handle(new SendSystemBroadcastCommand(title, message), CancellationToken.None);

        // Assert
        _notificationServiceMock.Verify(n => n.BroadcastAsync(title, message, It.IsAny<CancellationToken>()), Times.Once);
    }
}
