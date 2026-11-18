namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// DTO tóm tắt bài viết dùng cho danh sách bài viết công khai, CMS và tin liên quan.
/// </summary>
public sealed record ArticleSummaryDto(
    Guid Id,
    string Title,
    string Slug,
    string? Excerpt,
    string Status,
    AuthorSummaryDto Author,
    Guid? CompanyId,
    CoverImageDto? CoverImage,
    int ReadingTimeMinutes,
    long ViewCount,
    IReadOnlyList<CategorySummaryDto> Categories,
    DateTimeOffset? PublishedAt,
    DateTimeOffset CreatedAt);
