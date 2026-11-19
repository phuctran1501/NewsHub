namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record AiSummaryResultDto(
    IReadOnlyList<string> BulletPoints,
    string Sentiment,
    IReadOnlyList<string> ExtractedTags,
    float ConfidenceScore,
    string AiModel);
