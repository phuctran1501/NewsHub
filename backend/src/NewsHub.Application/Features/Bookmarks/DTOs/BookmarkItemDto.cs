namespace NewsHub.Application.Features.Bookmarks.DTOs;

public sealed record BookmarkItemDto(
    Guid ArticleId,
    string Title,
    string Slug,
    string? CoverImageUrl,
    DateTimeOffset SavedAt,
    string? AuthorName,
    string? CategoryName);
