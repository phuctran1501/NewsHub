namespace NewsHub.Application.Features.Admin.Queries.GetAdminAnalyticsOverview;

using MediatR;
using Microsoft.EntityFrameworkCore;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Application.Features.Admin.Constants;
using NewsHub.Application.Features.Admin.DTOs;
using NewsHub.Domain.Constants;
using NewsHub.Domain.Enums;
using NewsHub.Domain.Exceptions;

/// <summary>
/// Query yêu cầu tổng hợp 6 chỉ số phân tích vận hành toàn hệ thống.
/// </summary>
public sealed record GetAdminAnalyticsOverviewQuery : IRequest<AdminAnalyticsOverviewDto>;

public sealed class GetAdminAnalyticsOverviewQueryHandler(
    IApplicationDbContext dbContext,
    ICacheService cacheService,
    ICurrentUser currentUser) : IRequestHandler<GetAdminAnalyticsOverviewQuery, AdminAnalyticsOverviewDto>
{
    public async Task<AdminAnalyticsOverviewDto> Handle(
        GetAdminAnalyticsOverviewQuery request,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedException("Vui lòng đăng nhập để tiếp tục.", ErrorCodes.Unauthorized);
        }

        if (!currentUser.IsInRole(UserRole.Admin))
        {
            throw new ForbiddenException("Chỉ quản trị viên mới có quyền xem báo cáo phân tích tổng quan.", ErrorCodes.PermissionDenied);
        }

        // 1. Kiểm tra Redis cache (Cache-Aside)
        var cachedOverview = await cacheService.GetAsync<AdminAnalyticsOverviewDto>(
            AdminCacheKeys.AnalyticsOverview,
            cancellationToken);

        if (cachedOverview is not null)
        {
            return cachedOverview;
        }

        // 2. Tính toán 6 chỉ số cốt lõi từ cơ sở dữ liệu
        // Chỉ số 1: Tổng số bài viết theo trạng thái
        var totalArticles = await dbContext.Articles.CountAsync(cancellationToken);
        var draftArticles = await dbContext.Articles.CountAsync(a => a.Status == ArticleStatus.Draft, cancellationToken);
        var publishedArticles = await dbContext.Articles.CountAsync(a => a.Status == ArticleStatus.Published, cancellationToken);
        var archivedArticles = await dbContext.Articles.CountAsync(a => a.Status == ArticleStatus.Archived, cancellationToken);
        var articlesCount = new ArticlesCountSummaryDto(totalArticles, draftArticles, publishedArticles, archivedArticles);

        // Chỉ số 2: Số lượng bài viết xuất bản mới trong 7 và 30 ngày gần nhất
        var now = DateTimeOffset.UtcNow;
        var sevenDaysAgo = now.AddDays(-7);
        var thirtyDaysAgo = now.AddDays(-30);
        var last7Days = await dbContext.Articles.CountAsync(
            a => a.Status == ArticleStatus.Published && a.PublishedAt.HasValue && a.PublishedAt.Value >= sevenDaysAgo,
            cancellationToken);
        var last30Days = await dbContext.Articles.CountAsync(
            a => a.Status == ArticleStatus.Published && a.PublishedAt.HasValue && a.PublishedAt.Value >= thirtyDaysAgo,
            cancellationToken);
        var recentArticles = new RecentArticlesSummaryDto(last7Days, last30Days);

        // Chỉ số 3: Tổng số lượt đọc bài báo (cộng dồn từ ViewCount)
        var totalReads = await dbContext.Articles
            .AsNoTracking()
            .Select(a => (long?)a.ViewCount)
            .SumAsync(cancellationToken) ?? 0L;

        // Chỉ số 4: Top 5 bài viết được bookmark nhiều nhất
        var topBookmarkedGroups = await dbContext.UserBookmarks
            .AsNoTracking()
            .GroupBy(b => b.ArticleId)
            .Select(g => new { ArticleId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(cancellationToken);

        var bookmarkedArticleIds = topBookmarkedGroups.Select(x => x.ArticleId).ToList();
        var articlesMap = await dbContext.Articles
            .AsNoTracking()
            .Where(a => bookmarkedArticleIds.Contains(a.Id))
            .Select(a => new { a.Id, a.Title, a.Slug })
            .ToDictionaryAsync(a => a.Id, cancellationToken);

        var topBookmarkedArticles = topBookmarkedGroups
            .Where(x => articlesMap.ContainsKey(x.ArticleId))
            .Select(x => new TopBookmarkedArticleDto(
                x.ArticleId,
                articlesMap[x.ArticleId].Title,
                articlesMap[x.ArticleId].Slug,
                x.Count))
            .ToList();

        // Chỉ số 5: Top 5 nhà báo có nhiều bài xuất bản nhất
        var topAuthorGroups = await dbContext.Articles
            .AsNoTracking()
            .Where(a => a.Status == ArticleStatus.Published)
            .GroupBy(a => a.AuthorId)
            .Select(g => new { AuthorId = g.Key, PublishedCount = g.Count() })
            .OrderByDescending(x => x.PublishedCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var authorIds = topAuthorGroups.Select(x => x.AuthorId).ToList();
        var authorsMap = await dbContext.Users
            .AsNoTracking()
            .Where(u => authorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName, Email = (string)u.Email })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        var topJournalists = topAuthorGroups
            .Where(x => authorsMap.ContainsKey(x.AuthorId))
            .Select(x => new TopJournalistDto(
                x.AuthorId,
                authorsMap[x.AuthorId].FullName,
                authorsMap[x.AuthorId].Email,
                x.PublishedCount))
            .ToList();

        // Chỉ số 6: Top danh mục có nhiều bài viết nhất
        var topCategoryGroups = await dbContext.ArticleCategories
            .AsNoTracking()
            .GroupBy(ac => ac.CategoryId)
            .Select(g => new { CategoryId = g.Key, ArticleCount = g.Count() })
            .OrderByDescending(x => x.ArticleCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var categoryIds = topCategoryGroups.Select(x => x.CategoryId).ToList();
        var categoriesMap = await dbContext.Categories
            .AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var topCategories = topCategoryGroups
            .Where(x => categoriesMap.ContainsKey(x.CategoryId))
            .Select(x => new TopCategoryDto(
                x.CategoryId,
                categoriesMap[x.CategoryId].Name,
                x.ArticleCount))
            .ToList();

        var overview = new AdminAnalyticsOverviewDto(
            articlesCount,
            recentArticles,
            totalReads,
            topBookmarkedArticles,
            topJournalists,
            topCategories);

        // 3. Đưa vào Redis cache kèm TTL 15 phút
        await cacheService.SetAsync(
            AdminCacheKeys.AnalyticsOverview,
            overview,
            AdminCacheKeys.AnalyticsOverviewTtl,
            cancellationToken);

        return overview;
    }
}
