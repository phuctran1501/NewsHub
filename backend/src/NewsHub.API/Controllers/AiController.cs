namespace NewsHub.API.Controllers;

using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using NewsHub.API.Extensions;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Common.Models;
using NewsHub.Application.Features.Ai.DTOs;
using NewsHub.Application.Features.Ai.Queries.SearchArticlesByNlp;
using NewsHub.Application.Features.Ai.Queries.SuggestCategories;

[ApiController]
[Route("api/v1/ai")]
public class AiController(
    ISender mediator,
    ICorrelationIdProvider correlationIdProvider) : ControllerBase
{
    /// <summary>
    /// Gợi ý chuyên mục tự động cho bài viết sử dụng AI (Human-in-the-loop).
    /// Giới hạn: 10 requests / 1 phút / tài khoản.
    /// </summary>
    [HttpPost("categorize")]
    [Authorize(Roles = "Journalist,Admin")]
    [EnableRateLimiting(RateLimitingExtensions.AiCategorizePolicy)]
    public async Task<IActionResult> SuggestCategories(
        [FromBody] SuggestCategoriesRequest request,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new SuggestCategoriesQuery(request.Title, request.ContentSnippet);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }

    /// <summary>
    /// Tìm kiếm bài viết thông minh bằng ngôn ngữ tự nhiên (NLP Semantic Search & pgvector).
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchByNlp(
        [FromQuery] string q,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var correlationId = correlationIdProvider.GetCorrelationId();
        var query = new SearchArticlesByNlpQuery(q, limit);
        var result = await mediator.Send(query, cancellationToken);

        return Ok(ApiResponseFactory.Success(result, correlationId));
    }
}

public sealed record SuggestCategoriesRequest(string Title, string ContentSnippet);
