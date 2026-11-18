namespace NewsHub.Application.Features.Admin.Constants;

/// <summary>
/// Quản lý hằng số khóa cache và thời gian sống (TTL) cho module Admin.
/// </summary>
public static class AdminCacheKeys
{
    public const string AnalyticsOverview = "admin:analytics:overview";
    public static readonly TimeSpan AnalyticsOverviewTtl = TimeSpan.FromMinutes(15);
}
