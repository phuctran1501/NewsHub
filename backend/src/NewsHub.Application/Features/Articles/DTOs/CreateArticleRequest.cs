namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Yêu cầu soạn thảo bài viết mới (trạng thái Draft).
/// </summary>
public sealed record CreateArticleRequest(
    string Title,
    string ContentHtml,
    Guid? CoverImageId = null,
    IReadOnlyList<Guid>? CategoryIds = null,
    Guid? CompanyId = null,
    string? SourceName = null,
    string? SourceUrl = null);
