using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NewsHub.Infrastructure.Services;
using StackExchange.Redis;
using Xunit;

namespace NewsHub.Application.Tests.Services;

public class RedisCacheServiceTests
{
    private readonly Mock<IConnectionMultiplexer> _mockMultiplexer;
    private readonly Mock<IDatabase> _mockDatabase;
    private readonly Mock<IServer> _mockServer;
    private readonly Mock<ILogger<RedisCacheService>> _mockLogger;
    private readonly RedisOptions _options;
    private readonly RedisCacheService _service;

    public RedisCacheServiceTests()
    {
        _mockMultiplexer = new Mock<IConnectionMultiplexer>();
        _mockDatabase = new Mock<IDatabase>();
        _mockServer = new Mock<IServer>();
        _mockLogger = new Mock<ILogger<RedisCacheService>>();

        _options = new RedisOptions
        {
            ConnectionString = "localhost:6379,abortConnect=false",
            InstanceName = "NewsHubTest:",
            DefaultExpirationMinutes = 15
        };

        var optionsWrapper = Options.Create(_options);

        _mockMultiplexer.Setup(m => m.IsConnected).Returns(true);
        _mockMultiplexer.Setup(m => m.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_mockDatabase.Object);

        var endPoint = new IPEndPoint(IPAddress.Loopback, 6379);
        _mockMultiplexer.Setup(m => m.GetEndPoints(It.IsAny<bool>())).Returns([endPoint]);
        _mockMultiplexer.Setup(m => m.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>())).Returns(_mockServer.Object);

        _mockServer.Setup(s => s.IsConnected).Returns(true);
        _mockServer.Setup(s => s.IsReplica).Returns(false);

        _service = new RedisCacheService(_mockMultiplexer.Object, optionsWrapper, _mockLogger.Object);
    }

    public sealed record TestCacheModel(Guid Id, string Name, int Value);

    [Fact]
    public async Task GetAsync_WhenKeyExists_ReturnsDeserializedObject()
    {
        // Arrange
        var expected = new TestCacheModel(Guid.NewGuid(), "Technology", 42);
        var json = JsonSerializer.Serialize(expected, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _mockDatabase
            .Setup(d => d.StringGetAsync("NewsHubTest:categories:tech", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        // Act
        var result = await _service.GetAsync<TestCacheModel>("categories:tech");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.Name, result.Name);
        Assert.Equal(expected.Value, result.Value);
    }

    [Fact]
    public async Task GetAsync_WhenKeyDoesNotExist_ReturnsNull()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync("NewsHubTest:missing", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        // Act
        var result = await _service.GetAsync<TestCacheModel>("missing");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenRedisThrowsException_ReturnsNullAndLogsWarning()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "Server down"));

        // Act
        var result = await _service.GetAsync<TestCacheModel>("error:key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetAsync_WhenRedisNotConnected_ReturnsNullWithoutCallingDatabase()
    {
        // Arrange
        _mockMultiplexer.Setup(m => m.IsConnected).Returns(false);

        // Act
        var result = await _service.GetAsync<TestCacheModel>("notconnected:key");

        // Assert
        Assert.Null(result);
        _mockDatabase.Verify(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task SetAsync_WithValidValue_CallsStringSetAsyncWithTtl()
    {
        // Arrange
        var model = new TestCacheModel(Guid.NewGuid(), "Articles", 100);
        var customTtl = TimeSpan.FromMinutes(60);

        _mockDatabase
            .Setup(d => d.StringSetAsync(
                "NewsHubTest:articles:1",
                It.IsAny<RedisValue>(),
                customTtl,
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _service.SetAsync("articles:1", model, customTtl);

        // Assert
        _mockDatabase.Verify(d => d.StringSetAsync(
            "NewsHubTest:articles:1",
            It.Is<RedisValue>(v => v.ToString().Contains("Articles")),
            customTtl,
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task SetAsync_WhenRedisThrowsException_LogsWarningWithoutCrashing()
    {
        // Arrange
        var model = new TestCacheModel(Guid.NewGuid(), "Failing", 0);
        _mockDatabase
            .Setup(d => d.StringSetAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisTimeoutException("Timeout", CommandStatus.WaitingToBeSent));

        // Act & Assert (Should not throw)
        var exception = await Record.ExceptionAsync(() => _service.SetAsync("fail:key", model));
        Assert.Null(exception);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheHit_ReturnsCachedItemWithoutCallingFactory()
    {
        // Arrange
        var cached = new TestCacheModel(Guid.NewGuid(), "Cached", 1);
        var json = JsonSerializer.Serialize(cached, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        _mockDatabase
            .Setup(d => d.StringGetAsync("NewsHubTest:item:1", It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue(json));

        var factoryCalled = false;

        // Act
        var result = await _service.GetOrCreateAsync("item:1", ct =>
        {
            factoryCalled = true;
            return Task.FromResult(new TestCacheModel(Guid.NewGuid(), "Fresh", 2));
        });

        // Assert
        Assert.False(factoryCalled);
        Assert.Equal("Cached", result.Name);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenCacheMiss_CallsFactoryAndCachesResult()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.StringGetAsync("NewsHubTest:item:2", It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisValue.Null);

        var fresh = new TestCacheModel(Guid.NewGuid(), "Fresh", 99);

        // Act
        var result = await _service.GetOrCreateAsync("item:2", ct => Task.FromResult(fresh), TimeSpan.FromMinutes(10));

        // Assert
        Assert.Equal(fresh.Id, result.Id);
        _mockDatabase.Verify(d => d.StringSetAsync(
            "NewsHubTest:item:2",
            It.IsAny<RedisValue>(),
            TimeSpan.FromMinutes(10),
            It.IsAny<bool>(),
            It.IsAny<When>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenRedisOffline_FallsBackToFactoryDirectly()
    {
        // Arrange
        _mockMultiplexer.Setup(m => m.IsConnected).Returns(false);
        var fresh = new TestCacheModel(Guid.NewGuid(), "OfflineFallback", 100);

        // Act
        var result = await _service.GetOrCreateAsync("offline:key", ct => Task.FromResult(fresh));

        // Assert
        Assert.Equal("OfflineFallback", result.Name);
    }

    [Fact]
    public async Task RemoveAsync_CallsKeyDeleteAsyncWithPrefixedKey()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.KeyDeleteAsync("NewsHubTest:article:delete", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        await _service.RemoveAsync("article:delete");

        // Assert
        _mockDatabase.Verify(d => d.KeyDeleteAsync("NewsHubTest:article:delete", It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task RemoveByPrefixAsync_ScansKeysAndDeletesInBatches()
    {
        // Arrange
        var keys = new RedisKey[] { "NewsHubTest:articles:1", "NewsHubTest:articles:2" };
        _mockServer
            .Setup(s => s.KeysAsync(
                It.IsAny<int>(),
                (RedisValue)"NewsHubTest:articles:*",
                It.IsAny<int>(),
                It.IsAny<long>(),
                It.IsAny<int>(),
                It.IsAny<CommandFlags>()))
            .Returns(ToAsyncEnumerable(keys));

        _mockDatabase
            .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(2);

        // Act
        await _service.RemoveByPrefixAsync("articles:");

        // Assert
        _mockDatabase.Verify(d => d.KeyDeleteAsync(
            It.Is<RedisKey[]>(k => k.Length == 2),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.KeyExistsAsync("NewsHubTest:exists", It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var exists = await _service.ExistsAsync("exists");

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenLockAvailable_ReturnsDisposableHandle()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.LockTakeAsync(
                "NewsHubTest:lock:article:slug",
                It.IsAny<RedisValue>(),
                TimeSpan.FromSeconds(5),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        _mockDatabase
            .Setup(d => d.LockRelease(
                "NewsHubTest:lock:article:slug",
                It.IsAny<RedisValue>(),
                It.IsAny<CommandFlags>()))
            .Returns(true);

        // Act
        var handle = await _service.AcquireLockAsync("article:slug", TimeSpan.FromSeconds(5));

        // Assert
        Assert.NotNull(handle);

        // Dispose should call LockRelease
        handle.Dispose();
        _mockDatabase.Verify(d => d.LockRelease(
            "NewsHubTest:lock:article:slug",
            It.IsAny<RedisValue>(),
            It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenLockBusyAndNoTimeout_ReturnsNull()
    {
        // Arrange
        _mockDatabase
            .Setup(d => d.LockTakeAsync(
                It.IsAny<RedisKey>(),
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CommandFlags>()))
            .ReturnsAsync(false);

        // Act
        var handle = await _service.AcquireLockAsync("busy:key", TimeSpan.FromSeconds(5));

        // Assert
        Assert.Null(handle);
    }

    [Fact]
    public async Task AcquireLockAsync_WhenRedisOffline_ReturnsNullSafely()
    {
        // Arrange
        _mockMultiplexer.Setup(m => m.IsConnected).Returns(false);

        // Act
        var handle = await _service.AcquireLockAsync("offline:lock", TimeSpan.FromSeconds(5));

        // Assert
        Assert.Null(handle);
    }

    private static async IAsyncEnumerable<T> ToAsyncEnumerable<T>(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            await Task.Yield();
            yield return item;
        }
    }
}

