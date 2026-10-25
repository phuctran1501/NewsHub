using NewsHub.Application.Common.Models;

namespace NewsHub.Application.Common.Interfaces;

/// <summary>
/// Abstraction cho tầng Application để phát các sự kiện thời gian thực tới người dùng qua SignalR.
/// </summary>
public interface INotificationService
{
    Task SendToUserAsync(Guid userId, NotificationDto notification, CancellationToken cancellationToken = default);

    Task SendToGroupAsync(string groupName, NotificationDto notification, CancellationToken cancellationToken = default);

    Task BroadcastAsync(string title, string message, CancellationToken cancellationToken = default);

    Task NotifyAiSummaryCompletedAsync(Guid userId, Guid articleId, string sentiment, string[] bulletPoints, CancellationToken cancellationToken = default);

    Task NotifyArticleStatusChangedAsync(Guid authorId, Guid articleId, string newStatus, CancellationToken cancellationToken = default);
}
