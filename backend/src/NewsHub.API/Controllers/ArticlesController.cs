namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Articles.Commands.ArchiveArticle;
using NewsHub.Application.Features.Articles.Commands.CreateArticle;
using NewsHub.Application.Features.Articles.Commands.DeleteArticle;
using NewsHub.Application.Features.Articles.Commands.GenerateDraftPreviewToken;
using NewsHub.Application.Features.Articles.Commands.PublishArticle;
using NewsHub.Application.Features.Articles.Commands.ReportArticle;
using NewsHub.Application.Features.Articles.Commands.UpdateArticle;
using NewsHub.Application.Features.Articles.DTOs;
using NewsHub.Application.Features.Articles.Queries.GetArticleBySlug;
using NewsHub.Application.Features.Articles.Queries.GetDraftArticleByPreviewToken;
using NewsHub.Application.Features.Articles.Queries.GetMyArticles;
using NewsHub.Application.Features.Articles.Queries.GetPublishedArticles;
using NewsHub.Application.Features.Articles.Queries.GetRelatedArticles;
using NewsHub.Application.Features.Companies.Commands.SubmitArticleToCompany;
using NewsHub.Domain.Enums;

[ApiController]
[Route("api/v1/articles")]
public class ArticlesController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Duyệt danh sách bài viết công khai đã xuất bản (Có phân trang, lọc và sắp xếp).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetPublishedArticles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? categorySlug = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] Guid? authorId = null,
        [FromQuery] Guid? companyId = null,
        [FromQuery] string? sortBy = "latest",
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetPublishedArticlesQuery(pageNumber, pageSize, categorySlug, searchTerm, authorId, companyId, sortBy);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Danh sách bài viết do phóng viên hiện tại tạo ra trong CMS (Mọi trạng thái, có phân trang).
    /// </summary>
    [HttpGet("my-articles")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> GetMyArticles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] ArticleStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new GetMyArticlesQuery(pageNumber, pageSize, status);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Paged(result, correlationId));
    }

    /// <summary>
    /// Xem trước bài viết nháp bằng token hợp lệ (2 giờ, kèm header no-index, no-store).
    /// </summary>
    [HttpGet("preview")]
    public async Task<IActionResult> GetDraftArticleByPreviewToken(
        [FromQuery] string token,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetDraftArticleByPreviewTokenQuery(token), cancellationToken);

        Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        Response.Headers["Cache-Control"] = "no-store";

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xem thông tin chi tiết một bài viết theo Slug hoặc định danh ID (Public, Cache Redis 1h).
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetArticleBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetArticleBySlugQuery(slug), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Gợi ý các bài viết liên quan theo khoảng cách cosine vector / cùng chuyên mục (Public, Cache Redis 1h).
    /// </summary>
    [HttpGet("{id:guid}/related")]
    public async Task<IActionResult> GetRelatedArticles(
        [FromRoute] Guid id,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetRelatedArticlesQuery(id, limit), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Soạn thảo bài viết mới ở trạng thái Nháp (Draft).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> CreateArticle(
        [FromBody] CreateArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new CreateArticleCommand(
            request.Title,
            request.ContentHtml,
            request.CoverImageId,
            request.CategoryIds,
            request.CompanyId,
            request.SourceName,
            request.SourceUrl);

        var result = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/articles/{result.Slug}", ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Cập nhật nội dung bài viết (Chỉ tác giả hoặc Admin).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> UpdateArticle(
        [FromRoute] Guid id,
        [FromBody] UpdateArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new UpdateArticleCommand(
            id,
            request.Title,
            request.ContentHtml,
            request.CoverImageId,
            request.CategoryIds,
            request.SourceName,
            request.SourceUrl);

        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xuất bản bài viết (Chuyển sang Published, kích hoạt Hangfire AI Job và thông báo SignalR).
    /// </summary>
    [HttpPost("{id:guid}/publish")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> PublishArticle(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new PublishArticleCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Lưu trữ bài viết (Chuyển sang Archived).
    /// </summary>
    [HttpPost("{id:guid}/archive")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> ArchiveArticle(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new ArchiveArticleCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xóa bài viết khỏi hệ thống (Chỉ tác giả hoặc Admin; chặn xóa bài tòa soạn đã xuất bản).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> DeleteArticle(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DeleteArticleCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Xóa bài viết thành công." }, correlationId));
    }

    /// <summary>
    /// Sinh token JWT xem trước bài nháp với thời hạn hiệu lực 2 giờ.
    /// </summary>
    [HttpPost("{id:guid}/preview-token")]
    [Authorize(Roles = "Journalist,Admin")]
    public async Task<IActionResult> GenerateDraftPreviewToken(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GenerateDraftPreviewTokenCommand(id), cancellationToken);

        return Created(result.PreviewUrl, ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Độc giả gửi báo cáo vi phạm nội dung bài viết (Rate limit: 5 lượt/giờ).
    /// </summary>
    [HttpPost("{id:guid}/report")]
    [Authorize]
    public async Task<IActionResult> ReportArticle(
        [FromRoute] Guid id,
        [FromBody] ReportArticleRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new ReportArticleCommand(id, request.Reason, request.Details);
        await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Báo cáo bài viết thành công. Ban biên tập sẽ tiến hành kiểm duyệt." }, correlationId));
    }

    /// <summary>
    /// Nhà báo nộp bài viết lên Tòa soạn để chờ kiểm duyệt và xuất bản.
    /// </summary>
    [HttpPost("{id:guid}/submit-to-company")]
    [Authorize(Roles = "Journalist")]
    public async Task<IActionResult> SubmitToCompany(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new SubmitArticleToCompanyCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success("Bài viết đã được gửi tới Ban Biên tập / Chủ tòa soạn phê duyệt.", correlationId));
    }
}
