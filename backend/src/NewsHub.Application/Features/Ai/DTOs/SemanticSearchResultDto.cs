namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record SemanticSearchResultDto(
    Guid ArticleId,
    string Title,
    string Slug,
    float SimilarityScore,
    DateTimeOffset? PublishedAt);
