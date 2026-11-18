namespace NewsHub.Application.Features.Admin.DTOs;

public record ArticlesCountSummaryDto(
    int Total,
    int Draft,
    int Published,
    int Archived);

public record RecentArticlesSummaryDto(
    int Last7Days,
    int Last30Days);

public record TopBookmarkedArticleDto(
    Guid ArticleId,
    string Title,
    string Slug,
    int BookmarkCount);

public record TopJournalistDto(
    Guid AuthorId,
    string FullName,
    string Email,
    int PublishedArticlesCount);

public record TopCategoryDto(
    Guid CategoryId,
    string Name,
    int ArticleCount);

/// <summary>
/// DTO chứa 6 chỉ số phân tích tổng quan toàn hệ thống dành cho Quản trị viên.
/// </summary>
public record AdminAnalyticsOverviewDto(
    ArticlesCountSummaryDto ArticlesCount,
    RecentArticlesSummaryDto RecentArticles,
    long TotalReads,
    IReadOnlyList<TopBookmarkedArticleDto> TopBookmarkedArticles,
    IReadOnlyList<TopJournalistDto> TopJournalists,
    IReadOnlyList<TopCategoryDto> TopCategories);
