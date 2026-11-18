namespace NewsHub.Application.Features.Articles.DTOs;

/// <summary>
/// Thông tin tóm tắt của tác giả bài viết.
/// </summary>
public sealed record AuthorSummaryDto(
    Guid Id,
    string FullName,
    string? AvatarUrl);
