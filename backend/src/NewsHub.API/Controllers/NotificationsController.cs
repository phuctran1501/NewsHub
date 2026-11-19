namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Notifications.Commands.DeleteNotification;
using NewsHub.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;
using NewsHub.Application.Features.Notifications.Commands.MarkNotificationAsRead;
using NewsHub.Application.Features.Notifications.Queries.GetMyNotifications;
using NewsHub.Application.Features.Notifications.Queries.GetUnreadNotificationCount;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách lịch sử thông báo cá nhân của người dùng (Phân trang, lọc theo trạng thái đã đọc/chưa đọc, sắp xếp mới nhất).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] bool? isRead = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetMyNotificationsQuery(pageNumber, pageSize, isRead);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Đếm nhanh số lượng thông báo chưa đọc của người dùng để hiển thị badge số lượng.
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetUnreadNotificationCountQuery();
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Đánh dấu một thông báo là đã đọc.
    /// </summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkNotificationAsRead(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new MarkNotificationAsReadCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã đánh dấu thông báo là đã đọc." }, correlationId));
    }

    /// <summary>
    /// Đánh dấu toàn bộ thông báo chưa đọc của người dùng thành đã đọc.
    /// </summary>
    [HttpPatch("read-all")]
    public async Task<IActionResult> MarkAllNotificationsAsRead(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var updatedCount = await mediator.Send(new MarkAllNotificationsAsReadCommand(), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã đánh dấu toàn bộ thông báo là đã đọc.", updatedCount }, correlationId));
    }

    /// <summary>
    /// Xóa một thông báo khỏi lịch sử của người dùng.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteNotification(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DeleteNotificationCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa thông báo thành công." }, correlationId));
    }
}
