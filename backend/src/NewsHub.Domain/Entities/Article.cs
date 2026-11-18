namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

/// <summary>
/// Thực thể bài viết tin tức trong hệ thống tòa soạn điện tử NewsHub.
/// </summary>
public class Article : IHasCreatedAt, IHasUpdatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid AuthorId { get; set; }
    public Guid? CompanyId { get; set; }
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public required string ContentHtml { get; set; }
    public ArticleStatus Status { get; set; } = ArticleStatus.Draft;
    public string? SourceName { get; set; }
    public string? SourceUrl { get; set; }
    public Guid? CoverImageId { get; set; }
    public int ReadingTimeMinutes { get; set; } = 1;
    public long ViewCount { get; set; } = 0;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation properties
    public User? Author { get; set; }
    public MediaFile? CoverImage { get; set; }
    public ICollection<ArticleCategory> ArticleCategories { get; set; } = new List<ArticleCategory>();
    public ICollection<ArticleReport> Reports { get; set; } = new List<ArticleReport>();
    public ICollection<ReadingHistory> ReadingHistories { get; set; } = new List<ReadingHistory>();
    public ICollection<UserBookmark> Bookmarks { get; set; } = new List<UserBookmark>();
}
