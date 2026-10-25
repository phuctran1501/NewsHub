using NewsHub.Domain.Enums;

namespace NewsHub.Application.Common.Models;

/// <summary>
/// Data Transfer Object mang payload thông báo gửi qua SignalR tới client.
/// </summary>
public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string Message,
    Guid? RelatedEntityId = null,
    string? TargetUrl = null,
    DateTime CreatedAt = default)
{
    public DateTime CreatedAt { get; init; } = CreatedAt == default ? DateTime.UtcNow : CreatedAt;
}
