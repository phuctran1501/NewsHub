namespace NewsHub.Application.Features.Companies.DTOs;

using NewsHub.Domain.Enums;

public sealed record EditorialArticleDto(
    Guid Id,
    string Title,
    string Slug,
    Guid AuthorId,
    string AuthorName,
    string? AuthorAvatarUrl,
    ArticleStatus Status,
    int ReadingTimeMinutes,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? PublishedAt
);
