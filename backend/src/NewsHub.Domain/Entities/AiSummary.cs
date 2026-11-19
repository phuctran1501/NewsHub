namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;

/// <summary>
/// Thực thể tóm tắt AI và phân tích sắc thái cảm xúc của bài viết.
/// </summary>
public class AiSummary
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid ArticleId { get; set; }
    public List<string> BulletPoints { get; set; } = [];
    public ArticleSentiment Sentiment { get; set; } = ArticleSentiment.Neutral;
    public List<string> ExtractedTags { get; set; } = [];
    public string AiModel { get; set; } = "gemini-3.5-flash-lite";
    public float ConfidenceScore { get; set; } = 1.0f;
    public DateTimeOffset ProcessedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    public Article? Article { get; set; }
}
