namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record CategorySuggestionDto(
    Guid CategoryId,
    string CategoryName,
    string Slug,
    float Confidence,
    string Reason);
