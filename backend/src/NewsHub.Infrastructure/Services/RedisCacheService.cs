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

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
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
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi đọc Redis Cache cho key {Key}. Tự động fallback về null", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (value is null)
        {
            return;
        }

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, bỏ qua lưu cache cho key: {Key}", key);
                return;
            }

            var db = _connectionMultiplexer.GetDatabase();
            var ttl = expiration ?? TimeSpan.FromMinutes(_options.DefaultExpirationMinutes);
            var serialized = JsonSerializer.Serialize(value, _jsonSerializerOptions);

            await db.StringSetAsync(BuildKey(key), serialized, ttl);
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi ghi Redis Cache cho key {Key}. Bỏ qua lỗi để tiếp tục luồng chính", key);
        }
    }

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var cached = await GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var result = await factory(cancellationToken);
        if (result is not null)
        {
            await SetAsync(key, result, expiration, cancellationToken);
        }

        return result;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, bỏ qua xóa cache cho key: {Key}", key);
                return;
            }

            var db = _connectionMultiplexer.GetDatabase();
            await db.KeyDeleteAsync(BuildKey(key));
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis Cache cho key {Key}", key);
        }
    }

    public async Task RemoveByPrefixAsync(string prefixKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefixKey);

        try
        {
            if (!_connectionMultiplexer.IsConnected)
            {
                _logger.LogWarning("Redis chưa kết nối, bỏ qua xóa cache theo tiền tố: {Prefix}", prefixKey);
                return;
            }

            var pattern = $"{BuildKey(prefixKey)}*";
            var db = _connectionMultiplexer.GetDatabase();

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
                        await db.KeyDeleteAsync(keys.ToArray());
                        keys.Clear();
                    }
                }

                if (keys.Count > 0)
                {
                    await db.KeyDeleteAsync(keys.ToArray());
                }
            }
        }
        catch (Exception ex) when (ex is RedisException or SocketException or TimeoutException)
        {
            _logger.LogWarning(ex, "Lỗi khi xóa Redis Cache theo tiền tố {Prefix}", prefixKey);
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
        private int _disposed;

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
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                _db.LockRelease(_lockKey, _lockValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi giải phóng khóa phân tán Redis {LockKey}", _lockKey);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            try
            {
                await _db.LockReleaseAsync(_lockKey, _lockValue);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Lỗi khi giải phóng khóa phân tán Redis bất đồng bộ {LockKey}", _lockKey);
            }
        }
    }
}

