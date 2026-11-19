namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

public class Notification : IHasCreatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public NotificationType Type { get; set; }
    public required string Title { get; set; }
    public required string Message { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTimeOffset? ReadAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
