namespace NewsHub.Application.Features.ReadingHistory.DTOs;

public sealed record ReadingHistoryItemDto(
    Guid HistoryId,
    Guid ArticleId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    DateTimeOffset ReadAt);
