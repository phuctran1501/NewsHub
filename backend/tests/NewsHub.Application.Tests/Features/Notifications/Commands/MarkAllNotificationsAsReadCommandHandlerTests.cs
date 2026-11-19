namespace NewsHub.Application.Tests.Features.Notifications.Commands;

using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Exceptions;
using Xunit;

public class MarkAllNotificationsAsReadCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private MarkAllNotificationsAsReadCommandHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new MarkAllNotificationsAsReadCommand(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }
}
