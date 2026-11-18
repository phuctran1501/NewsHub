namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

/// <summary>
/// Thực thể ghi nhận báo cáo vi phạm bài viết từ độc giả.
/// </summary>
public class ArticleReport : IHasCreatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ArticleId { get; set; }
    public Guid? ReporterId { get; set; }
    public ReportReason Reason { get; set; }
    public string? Details { get; set; }
    public ReportStatus Status { get; set; } = ReportStatus.Pending;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }

    // Navigation properties
    public Article? Article { get; set; }
    public User? Reporter { get; set; }
    public User? Resolver { get; set; }
}
