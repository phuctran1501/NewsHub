using System.Diagnostics;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NewsHub.Application.Common.Interfaces;
using StackExchange.Redis;

namespace NewsHub.Infrastructure.Services;

/// <summary>
/// Triển khai ICacheService sử dụng StackExchange.Redis.
/// Hỗ trợ Cache-Aside, tự động gắn tiền tố InstanceName, Distributed Lock và Graceful Fallback.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly RedisOptions _options;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public RedisCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<RedisOptions> options,
        ILogger<RedisCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _options = options.Value;
        _logger = logger;
        _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };
    }

    private string BuildKey(string key) => $"{_options.InstanceName}{key}";

    /// <summary>
    /// Escape các ký tự đặc biệt trong cú pháp Glob của Redis (*, ?, [, ], \)
    /// để tránh việc pattern matching vô tình áp dụng wildcard khi người dùng truyền ký tự đặc biệt trong prefixKey.
    /// </summary>
    private static string EscapeGlobPattern(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return string.Empty;
        }

        return prefix
            .Replace(@"\", @"\\")
            .Replace("*", @"\*")
            .Replace("?", @"\?")
            .Replace("[", @"\[")
            .Replace("]", @"\]");
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, bỏ qua cache cho key: {Key}", key);
                return default;
            }

            var db = _connectionMultiplexer.GetDatabase();
            var redisValue = await db.StringGetAsync(BuildKey(key));

            if (!redisValue.HasValue)
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(redisValue.ToString(), _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            // Giá trị lưu trong Redis bị hỏng JSON hoặc không khớp schema DTO.
            // Xử lý như Cache Miss để hệ thống tiếp tục gọi factory nạp dữ liệu mới.
            _logger.LogWarning(ex, "Dữ liệu cache trong Redis không thể deserialize cho key {Key}. Coi như Cache Miss", key);
            return default;
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc Redis Cache cho key {Key}. Tự động fallback về null", key);
            return default;
        }
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (value is null)
        {
            return false;
        }

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, bỏ qua lưu cache cho key: {Key}", key);
                return false;
            }

            var serialized = JsonSerializer.Serialize(value, _jsonSerializerOptions);
            var db = _connectionMultiplexer.GetDatabase();
            var ttl = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);

            return await db.StringSetAsync(BuildKey(key), serialized, ttl);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Không thể serialize đối tượng để lưu cache cho key {Key}. Bỏ qua ghi cache", key);
            return false;
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi Redis Cache cho key {Key}. Bỏ qua lỗi để tiếp tục luồng chính", key);
            return false;
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await factory(cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException($"Hàm factory tạo dữ liệu cho cache key '{key}' trả về null.");
        }

        await SetAsync(key, result, expiration, cancellationToken);
        return result;
    }

    public async Task<bool> RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, không thể xóa cache cho key: {Key}", key);
                return false;
            }

            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyDeleteAsync(BuildKey(key));
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis Cache cho key {Key}", key);
            return false;
        }
    }

    public async Task<bool> RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixKey);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, không thể xóa cache theo tiền tố: {Prefix}", prefixKey);
                return false;
            }

            // Escape tiền tố để chống diễn giải nhầm glob wildcards (*, ?, [, ])
            var escapedPrefix = EscapeGlobPattern(BuildKey(prefixKey));
            var pattern = $"{escapedPrefix}*";
            var db = _connectionMultiplexer.GetDatabase();
            var allSucceeded = true;

            foreach (var endpoint in _connectionMultiplexer.GetEndPoints())
            {
                var server = _connectionMultiplexer.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica)
                {
                    continue;
                }

                // Dùng SCAN không khóa luồng Redis (Non-blocking iteration)
                var keys = new List<RedisKey>();
                await foreach (var key in server.KeysAsync(pattern: pattern))
                {
                    keys.Add(key);
                    if (keys.Count >= 200)
                    {
                        // Thay vì gửi mảng KeyDeleteAsync(keys) (dễ bị CROSSSLOT trên Redis Cluster),
                        // dùng Task.WhenAll xóa từng key qua multiplexer pipeline an toàn với mọi hash slot.
                        await Task.WhenAll(keys.Select(k => db.KeyDeleteAsync(k)));
                        keys.Clear();
                    }
                }

                if (keys.Count > 0)
                {
                    await Task.WhenAll(keys.Select(k => db.KeyDeleteAsync(k)));
                }
            }

            return allSucceeded;
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis Cache theo tiền tố {Prefix}", prefixKey);
            return false;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, kiểm tra tồn tại trả về false cho key: {Key}", key);
                return false;
            }

            var db = _connectionMultiplexer.GetDatabase();
            return await db.KeyExistsAsync(BuildKey(key));
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi kiểm tra tồn tại Redis Cache cho key {Key}", key);
            return false;
        }
    }

    public async Task<IDisposable?> AcquireLockAsync(
        string key,
        TimeSpan expiration,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis offline, bỏ qua khóa phân tán cho {Key} (Fail-Open)", key);
                return null;
            }

            var db = _connectionMultiplexer.GetDatabase();
            var lockKey = BuildKey($"lock:{key}");
            var lockValue = Guid.NewGuid().ToString("N");
            var waitTimeout = timeout ?? TimeSpan.Zero;
            var stopwatch = Stopwatch.StartNew();

            while (!cancellationToken.IsCancellationRequested)
            {
                if (await db.LockTakeAsync(lockKey, lockValue, expiration))
                {
                    return new RedisDistributedLockHandle(db, lockKey, lockValue, _logger);
                }

                if (stopwatch.Elapsed >= waitTimeout)
                {
                    break;
                }

                await Task.Delay(200, cancellationToken);
            }

            return null;
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi chiếm khóa phân tán cho key {Key}. Fallback an toàn trả về null", key);
            return null;
        }
    }

    private sealed class RedisDistributedLockHandle : IDisposable, IAsyncDisposable
    {
        private readonly IDatabase _db;
        private readonly RedisKey _lockKey;
        private readonly RedisValue _lockValue;
        private readonly ILogger _logger;
        private int _released;

        public RedisDistributedLockHandle(
            IDatabase db,
            RedisKey lockKey,
            RedisValue lockValue,
            ILogger logger)
        {
            _db = db;
            _lockKey = lockKey;
            _lockValue = lockValue;
            _logger = logger;
        }

        public void Dispose()
        {
            if (Volatile.Read(ref _released) != 0)
            {
                return;
            }

            // Thử giải phóng khóa, cho phép thử lại 3 lần nếu gặp sự cố mạng tạm thời
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    _db.LockRelease(_lockKey, _lockValue);
                    Interlocked.Exchange(ref _released, 1);
                    return;
                }
                catch (Exception ex) when (attempt < 3 && ex is RedisException or SocketException or TimeoutException)
                {
                    _logger.LogWarning(ex, "Thử lại lần {Attempt} giải phóng khóa phân tán {LockKey}", attempt, _lockKey);
                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi không thể giải phóng khóa phân tán Redis {LockKey}. Khóa sẽ tự hết hạn theo TTL", _lockKey);
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Volatile.Read(ref _released) != 0)
            {
                return;
            }

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await _db.LockReleaseAsync(_lockKey, _lockValue);
                    Interlocked.Exchange(ref _released, 1);
                    return;
                }
                catch (Exception ex) when (attempt < 3 && ex is RedisException or SocketException or TimeoutException)
                {
                    _logger.LogWarning(ex, "Thử lại lần {Attempt} giải phóng khóa phân tán bất đồng bộ {LockKey}", attempt, _lockKey);
                    await Task.Delay(50);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Lỗi không thể giải phóng khóa phân tán Redis bất đồng bộ {LockKey}. Khóa sẽ tự hết hạn theo TTL", _lockKey);
                    break;
                }
            }
        }
    }
}
