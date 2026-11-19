namespace NewsHub.Application.Tests.Features.Notifications.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetUnreadNotificationCountQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetUnreadNotificationCountQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenUnreadNotificationsExist_ShouldReturnAccurateCount()
    {
        // Arrange
        var userId = Guid.NewGuid();
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _currentUserMock.Setup(u => u.Id).Returns(userId);

        var notifications = new List<Notification>
        {
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.CompanyInvitation,
                Title = "Chưa đọc 1",
                Message = "Nội dung",
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.SecurityAlert,
                Title = "Chưa đọc 2",
                Message = "Nội dung",
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ArticleStatusChanged,
                Title = "Đã đọc",
                Message = "Nội dung",
                IsRead = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // Khác User
                Type = NotificationType.SystemBroadcast,
                Title = "Chưa đọc của user khác",
                Message = "Nội dung",
                IsRead = false
            }
        };

        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetUnreadNotificationCountQuery(), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.UnreadCount);
    }
}
