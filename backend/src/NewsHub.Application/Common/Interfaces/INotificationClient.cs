using NewsHub.Application.Common.Models;

namespace NewsHub.Application.Common.Interfaces;

public interface INotificationClient
{
    Task ReceiveNotification(NotificationDto notification);
    Task AiSummaryCompleted(Guid articleId, string sentiment, string[] bulletPoints);
    Task ArticleStatusChanged(Guid articleId, string newStatus);
    Task SystemBroadcast(string title, string message);
}
