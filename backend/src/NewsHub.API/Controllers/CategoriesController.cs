namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Categories.Commands.CreateCategory;
using NewsHub.Application.Features.Categories.Commands.DeleteCategory;
using NewsHub.Application.Features.Categories.Commands.UpdateCategory;
using NewsHub.Application.Features.Categories.Queries.GetCategories;
using NewsHub.Application.Features.Categories.Queries.GetCategoryBySlug;
using NewsHub.Domain.Enums;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Lấy danh sách toàn bộ các chuyên mục tin tức đang hoạt động (Public, Cache Redis TTL 24h).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCategories(CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetCategoriesQuery(), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xem thông tin chi tiết một chuyên mục tin tức thông qua đường dẫn thân thiện (slug).
    /// </summary>
    [HttpGet("{slug}")]
    public async Task<IActionResult> GetCategoryBySlug(
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(new GetCategoryBySlugQuery(slug), cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Tạo mới một chuyên mục tin tức (Chỉ dành riêng cho quản trị viên Admin).
    /// </summary>
    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> CreateCategory(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var result = await mediator.Send(command, cancellationToken);

        return Created($"/api/v1/categories/{result.Slug}", ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Cập nhật thông tin chuyên mục tin tức (Chỉ dành riêng cho quản trị viên Admin).
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> UpdateCategory(
        [FromRoute] Guid id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var command = new UpdateCategoryCommand(id, request.Name, request.Description);
        var result = await mediator.Send(command, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Xóa chuyên mục tin tức (Chỉ dành riêng cho quản trị viên Admin).
    /// Ràng buộc toàn vẹn: Không cho phép xóa nếu chuyên mục đang chứa bài viết.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> DeleteCategory(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        await mediator.Send(new DeleteCategoryCommand(id), cancellationToken);

        return Ok(ApiResponseFactory.Success(new { message = "Đã xóa chuyên mục thành công." }, correlationId));
    }
}

public sealed record UpdateCategoryRequest(
    string? Name,
    string? Description);
