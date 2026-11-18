namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Bookmarks.Commands.AddBookmark;
using NewsHub.Application.Features.Bookmarks.Commands.ClearAllBookmarks;
using NewsHub.Application.Features.Bookmarks.Commands.RemoveBookmark;
using NewsHub.Application.Features.Bookmarks.Queries.GetMyBookmarks;
using NewsHub.Application.Features.ReadingHistory.Commands.ClearAllReadingHistory;
using NewsHub.Application.Features.ReadingHistory.Commands.DeleteReadingHistoryItem;
using NewsHub.Application.Features.ReadingHistory.Queries.GetMyReadingHistory;

[ApiController]
[Authorize]
[Route("api/v1/me")]
public class UserDashboardController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    // ==========================================
    // BOOKMARKS ENDPOINTS
    // ==========================================

    /// <summary>
    /// Xem danh sách bài viết đã lưu vào Bookmark của người dùng (Phân trang, sắp xếp mới nhất).
    /// </summary>
    [HttpGet("bookmarks")]
    public async Task<IActionResult> GetMyBookmarks(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetMyBookmarksQuery(pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Lưu bài viết vào danh sách Bookmark.
    /// </summary>
    [HttpPost("bookmarks/{articleId:guid}")]
    public async Task<IActionResult> AddBookmark(
        [FromRoute] Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new AddBookmarkCommand(articleId), cancellationToken);

        return StatusCode(201, ApiResponseFactory.Success(new { message = "Lưu bài viết vào danh sách yêu thích thành công." }, correlationId));
    }

    /// <summary>
    /// Bỏ lưu một bài viết khỏi Bookmark.
    /// </summary>
    [HttpDelete("bookmarks/{articleId:guid}")]
    public async Task<IActionResult> RemoveBookmark(
        [FromRoute] Guid articleId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new RemoveBookmarkCommand(articleId), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã bỏ lưu bài viết." }, correlationId));
    }

    /// <summary>
    /// Xóa toàn bộ danh sách bài viết đã bookmark của người dùng.
    /// </summary>
    [HttpDelete("bookmarks")]
    public async Task<IActionResult> ClearAllBookmarks(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var deletedCount = await mediator.Send(new ClearAllBookmarksCommand(), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa toàn bộ bài viết đã lưu.", deletedCount }, correlationId));
    }

    // ==========================================
    // READING HISTORY ENDPOINTS
    // ==========================================

    /// <summary>
    /// Xem lịch sử đọc bài báo của người dùng (Phân trang, sắp xếp gần đây nhất).
    /// </summary>
    [HttpGet("reading-history")]
    public async Task<IActionResult> GetMyReadingHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetMyReadingHistoryQuery(pageNumber, pageSize);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Xóa một mục cụ thể trong lịch sử đọc bài.
    /// </summary>
    [HttpDelete("reading-history/{historyId:guid}")]
    public async Task<IActionResult> DeleteReadingHistoryItem(
        [FromRoute] Guid historyId,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DeleteReadingHistoryItemCommand(historyId), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa bài viết khỏi lịch sử đọc." }, correlationId));
    }

    /// <summary>
    /// Xóa sạch toàn bộ lịch sử đọc bài của người dùng.
    /// </summary>
    [HttpDelete("reading-history")]
    public async Task<IActionResult> ClearAllReadingHistory(CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var deletedCount = await mediator.Send(new ClearAllReadingHistoryCommand(), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa toàn bộ lịch sử đọc bài.", deletedCount }, correlationId));
    }
}
