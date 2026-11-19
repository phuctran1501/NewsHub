namespace NewsHub.Domain.Entities;

using Pgvector;

/// <summary>
/// Thực thể lưu trữ vector nhúng 768 chiều phục vụ tìm kiếm ngữ nghĩa NLP.
/// </summary>
public class ArticleEmbedding
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ArticleId { get; set; }
    public Vector Embedding { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    public Article? Article { get; set; }
}
