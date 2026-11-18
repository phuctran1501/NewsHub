namespace NewsHub.Domain.Entities;

/// <summary>
/// Thực thể ghi nhận lịch sử đọc bài viết của người dùng đã đăng nhập.
/// </summary>
public class ReadingHistory
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public DateTimeOffset ReadAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Article? Article { get; set; }
}
