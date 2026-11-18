namespace NewsHub.Infrastructure.Jobs;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsHub.Application.Features.Articles.Constants;
using NewsHub.Infrastructure.Persistence;
using NewsHub.Infrastructure.Services;
using StackExchange.Redis;

/// <summary>
/// Hangfire Recurring Job chạy định kỳ mỗi 5 phút để đồng bộ lượt xem tích lũy từ Redis buffer vào PostgreSQL.
/// </summary>
public class SyncArticleViewsJob
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly NewsHubDbContext _dbContext;
    private readonly ILogger<SyncArticleViewsJob> _logger;
    private readonly RedisOptions _options;

    public SyncArticleViewsJob(
        IConnectionMultiplexer connectionMultiplexer,
        NewsHubDbContext dbContext,
        IOptions<RedisOptions> redisOptions,
        ILogger<SyncArticleViewsJob> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _dbContext = dbContext;
        _logger = logger;
        _options = redisOptions.Value;
    }

    private string BuildKey(string key) => $"{_options.InstanceName}{key}";

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_connectionMultiplexer.IsConnected)
        {
            _logger.LogWarning("Redis chưa kết nối, bỏ qua tác vụ SyncArticleViewsJob");
            return;
        }

        var db = _connectionMultiplexer.GetDatabase();
        var dirtyViewsSetKey = BuildKey(ArticleCacheKeys.DirtyViewsSet);
        var dirtyArticleIds = await db.SetMembersAsync(dirtyViewsSetKey);

        if (dirtyArticleIds.Length == 0)
        {
            return;
        }

        _logger.LogInformation("Bắt đầu đồng bộ lượt xem cho {Count} bài viết từ Redis về PostgreSQL", dirtyArticleIds.Length);

        foreach (var redisValue in dirtyArticleIds)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            if (!Guid.TryParse(redisValue.ToString(), out var articleId))
            {
                await db.SetRemoveAsync(dirtyViewsSetKey, redisValue);
                continue;
            }

            var bufferKey = BuildKey(ArticleCacheKeys.ViewsBuffer(articleId));
            var bufferValue = await db.StringGetDeleteAsync(bufferKey);

            if (bufferValue.HasValue && long.TryParse(bufferValue.ToString(), out var viewsDelta) && viewsDelta > 0)
            {
                try
                {
                    await _dbContext.Articles
                        .Where(a => a.Id == articleId)
                        .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + viewsDelta), cancellationToken);

                    _logger.LogDebug("Đã cộng {Delta} views cho ArticleId: {ArticleId}", viewsDelta, articleId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi cập nhật view_count cho ArticleId: {ArticleId}", articleId);
                    // Phục hồi lại buffer nếu update DB thất bại
                    await db.StringIncrementAsync(bufferKey, viewsDelta);
                    continue;
                }
            }

            await db.SetRemoveAsync(dirtyViewsSetKey, redisValue);
        }

        _logger.LogInformation("Hoàn tất tác vụ SyncArticleViewsJob");
    }
}
