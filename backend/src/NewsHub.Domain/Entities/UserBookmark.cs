namespace NewsHub.Domain.Entities;

/// <summary>
/// Thực thể bài viết được lưu (Bookmark) của người dùng đã đăng nhập.
/// Khóa chính kết hợp: (UserId, ArticleId).
/// </summary>
public class UserBookmark
{
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public DateTimeOffset SavedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Article? Article { get; set; }
}
