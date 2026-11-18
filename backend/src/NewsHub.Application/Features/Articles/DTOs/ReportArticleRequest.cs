namespace NewsHub.Application.Features.Articles.DTOs;

using NewsHub.Domain.Enums;

/// <summary>
/// Yêu cầu báo cáo vi phạm nội dung bài viết từ độc giả.
/// </summary>
public sealed record ReportArticleRequest(
    ReportReason Reason,
    string? Details = null);
