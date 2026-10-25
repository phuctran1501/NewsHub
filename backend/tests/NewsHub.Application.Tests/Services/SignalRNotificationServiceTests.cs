using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using NewsHub.API.Hubs;
using NewsHub.API.Services;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Domain.Enums;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<NotificationHub, INotificationClient>> _mockHubContext;
    private readonly Mock<IHubClients<INotificationClient>> _mockClients;
    private readonly Mock<INotificationClient> _mockClientProxy;
    private readonly Mock<ILogger<SignalRNotificationService>> _mockLogger;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<NotificationHub, INotificationClient>>();
        _mockClients = new Mock<IHubClients<INotificationClient>>();
        _mockClientProxy = new Mock<INotificationClient>();
        _mockLogger = new Mock<ILogger<SignalRNotificationService>>();

        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);

        _service = new SignalRNotificationService(_mockHubContext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task SendToUserAsync_ShouldPushNotificationToTargetUser()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var notification = new NotificationDto(
            Guid.NewGuid(),
            NotificationType.SystemBroadcast,
            "Welcome",
            "Welcome to NewsHub");

        // Act
        await _service.SendToUserAsync(userId, notification);

        // Assert
        _mockClients.Verify(c => c.User(userId.ToString()), Times.Once);
        _mockClientProxy.Verify(p => p.ReceiveNotification(notification), Times.Once);
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
