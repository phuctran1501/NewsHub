namespace NewsHub.Application.Features.Categories.DTOs;

public sealed record CategoryResponseDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    int ArticleCount,
    DateTimeOffset CreatedAt);
