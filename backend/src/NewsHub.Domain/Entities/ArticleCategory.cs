namespace NewsHub.Domain.Entities;

/// <summary>
/// Thực thể quan hệ nhiều-nhiều giữa Bài viết và Chuyên mục (có cờ đánh dấu AI gợi ý).
/// </summary>
public class ArticleCategory
{
    public Guid ArticleId { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsAiAssigned { get; set; } = false;

    // Navigation properties
    public Article? Article { get; set; }
    public Category? Category { get; set; }
}
