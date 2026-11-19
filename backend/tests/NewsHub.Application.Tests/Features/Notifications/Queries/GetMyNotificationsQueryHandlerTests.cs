namespace NewsHub.Application.Tests.Features.Notifications.Queries;

using MockQueryable.Moq;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Notifications.Queries.GetMyNotifications;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;
using Xunit;

public class GetMyNotificationsQueryHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();

    private GetMyNotificationsQueryHandler CreateHandler() =>
        new(_dbContextMock.Object, _currentUserMock.Object);

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedException()
    {
        // Arrange
        _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);
        var handler = CreateHandler();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None));
        Assert.Equal(ErrorCodes.Unauthorized, ex.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenNotificationsExist_ShouldReturnPagedNotificationsSortedDescending()
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
                Title = "Cũ hơn",
                Message = "Nội dung cũ",
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10),
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ArticleStatusChanged,
                Title = "Mới nhất",
                Message = "Nội dung mới",
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = true
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(), // Notification của user khác
                Type = NotificationType.SecurityAlert,
                Title = "Của người khác",
                Message = "Không được thấy",
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = false
            }
        };

        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(new GetMyNotificationsQuery(PageNumber: 1, PageSize: 10), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal("Mới nhất", result.Items[0].Title);
        Assert.Equal("Cũ hơn", result.Items[1].Title);
    }

    [Fact]
    public async Task Handle_WithIsReadFilter_ShouldFilterCorrectly()
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
                Title = "Chưa đọc",
                Message = "Nội dung",
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = false
            },
            new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Type = NotificationType.ArticleStatusChanged,
                Title = "Đã đọc",
                Message = "Nội dung",
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = true
            }
        };

        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act - Lọc chưa đọc
        var unreadResult = await handler.Handle(new GetMyNotificationsQuery(IsRead: false), CancellationToken.None);
        // Act - Lọc đã đọc
        var readResult = await handler.Handle(new GetMyNotificationsQuery(IsRead: true), CancellationToken.None);

        // Assert
        Assert.Single(unreadResult.Items);
        Assert.Equal("Chưa đọc", unreadResult.Items[0].Title);

        Assert.Single(readResult.Items);
        Assert.Equal("Đã đọc", readResult.Items[0].Title);
    }

    [Fact]
    public async Task Handle_WhenOffsetExceedsTotalCount_ShouldReturnEmptyPagedList()
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
                Title = "Thông báo 1",
                Message = "Nội dung",
                CreatedAt = DateTimeOffset.UtcNow,
                IsRead = false
            }
        };

        _dbContextMock.Setup(db => db.Notifications).Returns(notifications.BuildMockDbSet().Object);

        var handler = CreateHandler();

        // Act (Page 5, trong khi chỉ có 1 item)
        var result = await handler.Handle(new GetMyNotificationsQuery(PageNumber: 5, PageSize: 10), CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalCount);
        Assert.Empty(result.Items);
    }
}
