namespace NewsHub.Domain.Entities;

using NewsHub.Domain.Enums;
using NewsHub.Domain.Interfaces;

public class MediaFile : IHasCreatedAt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string PublicId { get; set; }
    public required string Url { get; set; }
    public required string OriginalFileName { get; set; }
    public required string Format { get; set; }
    public long Bytes { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public MediaFolder Folder { get; set; }
    public Guid UploadedByUserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property
    public User? UploadedByUser { get; set; }
    public ICollection<Article> CoverArticles { get; set; } = new HashSet<Article>();
}
