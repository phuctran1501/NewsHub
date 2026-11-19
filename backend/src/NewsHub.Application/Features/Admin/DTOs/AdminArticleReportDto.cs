namespace NewsHub.Application.Features.Admin.DTOs;

using NewsHub.Domain.Enums;

public record AdminReportUserSummaryDto(
    Guid Id,
    string FullName,
    string Email);

/// <summary>
/// DTO trả về chi tiết báo cáo vi phạm bài viết trong danh sách quản trị kiểm duyệt.
/// </summary>
public record AdminArticleReportDto(
    Guid Id,
    Guid ArticleId,
    string ArticleTitle,
    string ArticleSlug,
    AdminReportUserSummaryDto? Reporter,
    ReportReason Reason,
    string? Details,
    ReportStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ResolvedAt,
    Guid? ResolvedBy);
