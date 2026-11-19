namespace NewsHub.Application.Features.Ai.DTOs;

public sealed record ArticleAiSummaryDto(
    string Status,
    IReadOnlyList<string>? BulletPoints = null,
    string? Sentiment = null,
    IReadOnlyList<string>? ExtractedTags = null,
    float? ConfidenceScore = null,
    DateTimeOffset? ProcessedAt = null);
