namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Yêu cầu cập nhật nội dung bài viết.
/// </summary>
public sealed record UpdateArticleRequest(
    string? Title = null,
    string? ContentHtml = null,
    Guid? CoverImageId = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    string? SourceName = null,
    string? SourceUrl = null);
