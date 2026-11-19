namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record SuggestCategoriesResponseDto(
    IReadOnlyList<CategorySuggestionDto> Suggestions);
