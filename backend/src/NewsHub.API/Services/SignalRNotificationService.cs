using Microsoft.AspNetCore.SignalR;
using NewsHub.API.Hubs;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;

namespace NewsHub.API.Services;

/// <summary>
/// Triển khai INotificationService sử dụng IHubContext của SignalR để đẩy thông báo thời gian thực.
/// </summary>
public class SignalRNotificationService(
    IHubContext<NotificationHub, INotificationClient> hubContext,
    ILogger<SignalRNotificationService> logger) : INotificationService
{
    public async Task SendToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Đang gửi thông báo SignalR tới UserId: {UserId}, Tiêu đề: {Title}", userId, notification.Title);
        await hubContext.Clients.User(userId.ToString()).ReceiveNotification(notification);
    }

    public async Task SendToGroupAsync(string groupName, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Đang gửi thông báo SignalR tới Group: {GroupName}, Tiêu đề: {Title}", groupName, notification.Title);
        await hubContext.Clients.Group(groupName).ReceiveNotification(notification);
    }

    public async Task BroadcastAsync(string title, string message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Đang phát sóng thông báo SignalR toàn hệ thống: {Title}", title);
        await hubContext.Clients.All.SystemBroadcast(title, message);
    }

    public async Task NotifyAiSummaryCompletedAsync(Guid userId, Guid articleId, string sentiment, string[] bulletPoints, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Đang gửi sự kiện AiSummaryCompleted tới UserId: {UserId} cho ArticleId: {ArticleId}", userId, articleId);
        await hubContext.Clients.User(userId.ToString()).AiSummaryCompleted(articleId, sentiment, bulletPoints);
    }

    public async Task NotifyArticleStatusChangedAsync(Guid authorId, Guid articleId, string newStatus, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Đang gửi sự kiện ArticleStatusChanged tới AuthorId: {AuthorId} cho ArticleId: {ArticleId}, Trạng thái mới: {Status}",
            authorId, articleId, newStatus);

        // Gửi riêng cho tác giả
        await hubContext.Clients.User(authorId.ToString()).ArticleStatusChanged(articleId, newStatus);

        // Đồng thời bắn vào phòng theo dõi bài viết (nếu có client đang mở xem bài viết đó)
        var roomName = $"Article_{articleId}";
        await hubContext.Clients.Group(roomName).ArticleStatusChanged(articleId, newStatus);
    }
}
