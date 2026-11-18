namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// DTO chi tiết bài viết đầy đủ (bao gồm nội dung HTML đã được sanitize và thông tin nguồn tin).
/// </summary>
public sealed record ArticleDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string ContentHtml,
    string Status,
    AuthorSummaryDto Author,
    Guid? CompanyId,
    string? SourceName,
    string? SourceUrl,
    CoverImageDto? CoverImage,
    int ReadingTimeMinutes,
    long ViewCount,
    IReadOnlyList<CategorySummaryDto> Categories,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
