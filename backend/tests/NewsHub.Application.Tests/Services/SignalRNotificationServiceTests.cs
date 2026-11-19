using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable.Moq;
using Moq;
using NewsHub.API.Hubs;
using NewsHub.API.Services;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Entities;
using NewsHub.Domain.Enums;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext;
    private readonly Mock<IHubClients<INotificationClient>> _mockClients;
    private readonly Mock<INotificationClient> _mockClientProxy;
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IApplicationDbContext> _mockDbContext;
    private readonly List<Notification> _notificationsList = [];
    private readonly Mock<ILogger<SignalRNotificationService>> _mockLogger;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        _mockClients = new Mock<IHubClients<INotificationClient>>();
        _mockClientProxy = new Mock<INotificationClient>();
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockDbContext = new Mock<IApplicationDbContext>();
        _mockLogger = new Mock<ILogger<SignalRNotificationService>>();

        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

        _mockScopeFactory.Setup(f => f.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IApplicationDbContext))).Returns(_mockDbContext.Object);

        var mockDbSet = _notificationsList.BuildMockDbSet();
        mockDbSet.Setup(m => m.Add(It.IsAny<Notification>())).Callback<Notification>(_notificationsList.Add);
        _mockDbContext.Setup(db => db.Notifications).Returns(mockDbSet.Object);

        _service = new SignalRNotificationService(_mockHubContext.Object, _mockScopeFactory.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendToUserAsync_ShouldPersistToDatabaseAndPushToTargetUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notificationId = Guid.CreateVersion7();
        var notification = new NotificationDto(
            notificationId,
            NotificationType.SystemBroadcast,
            "Welcome",
            "Welcome to NewsHub",
            RelatedEntityId: null,
            TargetUrl: "/news");

        // Act
        await _service.SendToUserAsync(userId, notification);

        // Assert
        _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);
        _mockClientProxy.Verify(p => p.ReceiveNotification(notification), Times.Once);

        _mockDbContext.Verify(db => db.Notifications.Add(It.Is<Notification>(n =>
            n.Id == notificationId &&
            n.UserId == userId &&
            n.Title == "Welcome" &&
            n.Message == "Welcome to NewsHub" &&
            n.TargetUrl == "/news" &&
            !n.IsRead)), Times.Once);
        _mockDbContext.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.Single(_notificationsList);
    }

    [Fact]
    public async Task SendToUserAsync_WhenDatabaseThrowsException_ShouldStillPushSignalRNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new NotificationDto(
            Guid.NewGuid(),
            NotificationType.ArticleStatusChanged,
            "Title",
            "Message");

        _mockDbContext.Setup(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection error"));

        // Act
        await _service.SendToUserAsync(userId, notification);

        // Assert
        _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);
        _mockClientProxy.Verify(p => p.ReceiveNotification(notification), Times.Once);
    }

    [Fact]
    public async Task SendToUserAsync_WhenSignalRThrowsException_ShouldNotThrowException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new NotificationDto(
            Guid.NewGuid(),
            NotificationType.SecurityAlert,
            "Warning",
            "Security alert");

        _mockClientProxy.Setup(p => p.ReceiveNotification(It.IsAny<NotificationDto>()))
            .ThrowsAsync(new HubException("SignalR timeout"));

        // Act & Assert (Should not throw)
        var exception = await Record.ExceptionAsync(() => _service.SendToUserAsync(userId, notification));
        Assert.Null(exception);
    }

    [Fact]
    public async Task SendToGroupAsync_ShouldPushNotificationToTargetGroup()
    {
        // Arrange
        var groupName = "Role_Journalist";
        var notification = new NotificationDto(
            Guid.NewGuid(),
            NotificationType.ArticleStatusChanged,
            "Article Published",
            "Your article has been approved and published.");

        // Act
        await _service.SendToGroupAsync(groupName, notification);

        // Assert
        _mockClients.Verify(c => c.Group(groupName), Times.Once);
        _mockClientProxy.Verify(p => p.ReceiveNotification(notification), Times.Once);
    }

    [Fact]
    public async Task BroadcastAsync_ShouldBroadcastMessageToAllClients()
    {
        // Arrange
        var title = "Bảo trì hệ thống";
        var message = "Hệ thống sẽ bảo trì trong 10 phút.";

        // Act
        await _service.BroadcastAsync(title, message);

        // Assert
        _mockClients.Verify(c => c.All, Times.Once);
        _mockClientProxy.Verify(p => p.SystemBroadcast(title, message), Times.Once);
    }

    [Fact]
    public async Task NotifyAiSummaryCompletedAsync_ShouldPushToUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var sentiment = "Positive";
        var bulletPoints = new[] { "Điểm 1", "Điểm 2" };

        // Act
        await _service.NotifyAiSummaryCompletedAsync(userId, articleId, sentiment, bulletPoints);

        // Assert
        _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);
        _mockClientProxy.Verify(p => p.AiSummaryCompleted(articleId, sentiment, bulletPoints), Times.Once);
    }

    [Fact]
    public async Task NotifyArticleStatusChangedAsync_ShouldPushToAuthorAndArticleRoom()
    {
        // Arrange
        var authorId = Guid.NewGuid();
        var articleId = Guid.NewGuid();
        var newStatus = "Published";

        // Act
        await _service.NotifyArticleStatusChangedAsync(authorId, articleId, newStatus);

        // Assert
        _mockClients.Verify(c => c.User(authorId.ToString()), Times.Once);
        _mockClients.Verify(c => c.Group($"Article_{articleId}"), Times.Once);
        _mockClientProxy.Verify(p => p.ArticleStatusChanged(articleId, newStatus), Times.Exactly(2));
    }
}
