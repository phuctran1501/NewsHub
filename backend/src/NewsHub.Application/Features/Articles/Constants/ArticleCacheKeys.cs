namespace NewsHub.Application.Features.Articles.Constants;

public static class ArticleCacheKeys
{
    public static string ArticleSlug(string slug) => $"articles:slug:{slug.ToLowerInvariant()}";
    public static string ArticleDetail(Guid id) => $"articles:detail:{id}";
    public static string ArticleRelated(Guid id) => $"articles:related:{id}";

    // Debounced View Counter
    public static string ViewsBuffer(Guid id) => $"article:{id}:views_buffer";
    public const string DirtyViewsSet = "articles:dirty_views";

    // Rate Limiting
    public static string ReportRateLimit(Guid userId) => $"ratelimit:report:{userId}:{DateTime.UtcNow:yyyyMMddHH}";
}
