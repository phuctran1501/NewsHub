namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Thông tin tóm tắt của chuyên mục gán cho bài viết.
/// </summary>
public sealed record CategorySummaryDto(
    Guid Id,
    string Name,
    string Slug);
