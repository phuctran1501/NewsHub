namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Thông tin tệp tin ảnh bìa bài viết.
/// </summary>
public sealed record CoverImageDto(
    Guid Id,
    string Url,
    int? Width,
    int? Height,
    string? Format);
