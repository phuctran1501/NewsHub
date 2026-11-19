namespace NewsHub.API.Controllers;

using System.Text.Json.Serialization;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Admin.Commands.ResolveArticleReport;
using NewsHub.Application.Features.Admin.Commands.SendSystemBroadcast;
using NewsHub.Application.Features.Admin.Commands.ToggleUserActiveStatus;
using NewsHub.Application.Features.Admin.Commands.ReviewCompanyStatus;
using NewsHub.Application.Features.Admin.Commands.EmergencyAssignCompanyOwner;
using NewsHub.Application.Features.Admin.Queries.GetAdminAnalyticsOverview;
using NewsHub.Application.Features.Admin.Queries.GetAdminUsers;
using NewsHub.Application.Features.Admin.Queries.GetArticleReports;
using NewsHub.Application.Features.Admin.Queries.GetPendingCompanies;
using NewsHub.Domain.Enums;

public sealed record ToggleUserStatusRequest(
    [property: JsonRequired] bool IsActive,
    string? LockReason = null);

public sealed record SendBroadcastRequest(
    string Title,
    string Message);

public sealed record ResolveArticleReportRequest(
    [property: JsonConverter(typeof(JsonStringEnumConverter))]
    [property: JsonRequired]
    ReportStatus Status,
    bool ArchiveArticle = false,
    string? AdminNote = null);

public sealed record ReviewCompanyStatusRequest(
    [property: JsonRequired] string Status,
    string? RejectionReason = null);

public sealed record EmergencyAssignCompanyOwnerRequest(
    [property: JsonRequired] Guid NewOwnerId,
    [property: JsonRequired] string Reason);

/// <summary>
/// Trung tâm chỉ huy (Command Center) quản trị hệ thống và theo dõi phân tích số liệu toàn diện NewsHub.
/// Toàn bộ endpoint được bảo vệ nghiêm ngặt bằng vai trò Admin.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin")]
public class AdminController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    // ==========================================
    // 1. SYSTEM ANALYTICS
    // ==========================================

    /// <summary>
    /// Xem báo cáo phân tích tổng quan toàn hệ thống (6 chỉ số cốt lõi, Redis Cache-Aside 15 phút).
    /// </summary>
    [HttpGet("analytics/overview")]
    public async Task<IActionResult> GetAnalyticsOverview(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetAdminAnalyticsOverviewQuery(), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    // ==========================================
    // 2. USER & ACCESS MANAGEMENT
    // ==========================================

    /// <summary>
    /// Xem danh sách người dùng toàn hệ thống (Phân trang, lọc theo vai trò, trạng thái kích hoạt, tìm kiếm).
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetAdminUsersQuery(pageNumber, pageSize, role, isActive, searchTerm);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Khóa hoặc mở khóa tài khoản người dùng (Thu hồi toàn bộ Refresh Token khi khóa).
    /// </summary>
    [HttpPatch("users/{userId:guid}/status")]
    public async Task<IActionResult> ToggleUserStatus(
        [FromRoute] Guid userId,
        [FromBody] ToggleUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new ToggleUserActiveStatusCommand(userId, request.IsActive, request.LockReason);
        await mediator.Send(command, cancellationToken);

        var message = request.IsActive
            ? "Đã mở khóa tài khoản thành công."
            : "Đã khóa tài khoản người dùng thành công.";

        return Ok(ApiResponseFactory.Success(new { message, userId, isActive = request.IsActive }, correlationId));
    }

    // ==========================================
    // 3. SYSTEM BROADCAST
    // ==========================================

    /// <summary>
    /// Phát sóng thông báo khẩn cấp tới toàn bộ người dùng online qua SignalR.
    /// </summary>
    [HttpPost("notifications/broadcast")]
    public async Task<IActionResult> BroadcastNotification(
        [FromBody] SendBroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new SendSystemBroadcastCommand(request.Title, request.Message);
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã phát sóng thông báo tới toàn bộ người dùng online." }, correlationId));
    }

    // ==========================================
    // 4. CONTENT MODERATION & REPORTS
    // ==========================================

    /// <summary>
    /// Duyệt danh sách báo cáo vi phạm bài viết (Phân trang, lọc theo trạng thái và bài viết).
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetArticleReports(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ReportStatus? status = null,
        [FromQuery] Guid? articleId = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetArticleReportsQuery(pageNumber, pageSize, status, articleId);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Xử lý báo cáo vi phạm bài viết (Resolved / Dismissed, tùy chọn gỡ bài vi phạm sang Archived).
    /// </summary>
    [HttpPatch("reports/{id:guid}")]
    public async Task<IActionResult> ResolveArticleReport(
        [FromRoute] Guid id,
        [FromBody] ResolveArticleReportRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new ResolveArticleReportCommand(id, request.Status, request.ArchiveArticle, request.AdminNote);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    // ==========================================
    // 4. COMPANY MANAGEMENT & AUDIT
    // ==========================================

    /// <summary>
    /// Xem danh sách các công ty / tòa soạn đang chờ Quản trị viên phê duyệt thành lập.
    /// </summary>
    [HttpGet("companies/pending")]
    public async Task<IActionResult> GetPendingCompanies(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetPendingCompaniesQuery(pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Thẩm định phê duyệt hoặc từ chối hồ sơ thành lập tòa soạn.
    /// </summary>
    [HttpPatch("companies/{companyId:guid}/status")]
    public async Task<IActionResult> ReviewCompanyStatus(
        [FromRoute] Guid companyId,
        [FromBody] ReviewCompanyStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new ReviewCompanyStatusCommand(companyId, request.Status, request.RejectionReason);
        await mediator.Send(command, cancellationToken);

        var message = request.Status == "Approved"
            ? "Đã phê duyệt thành lập tòa soạn thành công."
            : "Đã từ chối hồ sơ thành lập tòa soạn.";

        return Ok(ApiResponseFactory.Success(message, correlationId));
    }

    /// <summary>
    /// Chỉ định Chủ sở hữu (Owner) thay thế khẩn cấp cho tòa soạn khi Owner cũ bị khóa hoặc vi phạm.
    /// </summary>
    [HttpPatch("companies/{companyId:guid}/assign-owner")]
    public async Task<IActionResult> EmergencyAssignCompanyOwner(
        [FromRoute] Guid companyId,
        [FromBody] EmergencyAssignCompanyOwnerRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new EmergencyAssignCompanyOwnerCommand(companyId, request.NewOwnerId, request.Reason);
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success("Đã chỉ định Chủ sở hữu thay thế thành công.", correlationId));
    }
}
