namespace NewsHub.Application.Features.Notifications.DTOs;

using System.Text.Json.Serialization;
using NewsHub.Domain.Enums;

public sealed record NotificationItemDto(
    Guid Id,
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    NotificationType Type,
    string Title,
    string Message,
    Guid? RelatedEntityId,
    string? TargetUrl,
    bool IsRead,
    DateTimeOffset? ReadAt,
    DateTimeOffset CreatedAt);
