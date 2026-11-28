using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NewsHub.Application.Common.Interfaces;
using NewsHub.Infrastructure.Services;

namespace NewsHub.Application.Tests.Services;

public sealed class MarketTickerServiceTests
{
    [Fact]
    public async Task GetMarketTickerAsync_WhenUpstreamsSucceed_ParsesAllLiveIndices()
    {
        var handler = new StubHttpMessageHandler(req =>
        {
            var url = req.RequestUri?.ToString() ?? string.Empty;
            if (url.Contains("vndirect.com.vn", StringComparison.OrdinalIgnoreCase))
            {
                const string vnJson = """
                {
                  "data": [
                    { "code": "VNINDEX", "close": 1310.55, "pctChange": 0.85 },
                    { "code": "VN30", "close": 1380.20, "pctChange": 1.12 },
                    { "code": "HNX", "close": 238.40, "pctChange": -0.35 },
                    { "code": "UPCOM", "close": 95.10, "pctChange": 0.25 }
                  ]
                }
                """;
                return JsonResponse(vnJson);
            }

            if (url.Contains("open.er-api.com", StringComparison.OrdinalIgnoreCase))
            {
                const string fxJson = """{ "result": "success", "rates": { "VND": 25450.4 } }""";
                return JsonResponse(fxJson);
            }

            if (url.Contains("sjc.com.vn", StringComparison.OrdinalIgnoreCase))
            {
                const string sjcJson = """
                {
                  "success": true,
                  "data": [
                    { "TypeName": "Vàng SJC 1L, 10L, 1KG", "BuyValue": 83000000, "SellValue": 85200000 }
                  ]
                }
                """;
                return JsonResponse(sjcJson);
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var cacheMock = CreatePassThroughCacheMock();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var httpClient = new HttpClient(handler);
        var service = new MarketTickerService(
            httpClient,
            cacheMock.Object,
            memoryCache,
            NullLogger<MarketTickerService>.Instance);

        var result = await service.GetMarketTickerAsync();

        Assert.True(result.IsLiveData);
        Assert.Equal(6, result.Items.Count);
        Assert.All(result.Items, item => Assert.True(item.IsLive));

        var vnIndex = result.Items.Single(x => x.Symbol == "VN-INDEX");
        Assert.Equal("1,310.55", vnIndex.Value);
        Assert.Equal("+0.85%", vnIndex.Change);
        Assert.True(vnIndex.IsPositive);

        var hnx = result.Items.Single(x => x.Symbol == "HNX-INDEX");
        Assert.Equal("238.40", hnx.Value);
        Assert.Equal("-0.35%", hnx.Change);
        Assert.False(hnx.IsPositive);

        var usdVnd = result.Items.Single(x => x.Symbol == "USD/VND");
        Assert.Equal("25,450", usdVnd.Value);

        var sjc = result.Items.Single(x => x.Symbol == "SJC 9999");
        Assert.Equal("85.20tr", sjc.Value);
    }

    [Fact]
    public async Task GetMarketTickerAsync_WhenAllUpstreamsFail_ReturnsSafeFallbackWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
        var cacheMock = CreatePassThroughCacheMock();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var httpClient = new HttpClient(handler);
        var service = new MarketTickerService(
            httpClient,
            cacheMock.Object,
            memoryCache,
            NullLogger<MarketTickerService>.Instance);

        var result = await service.GetMarketTickerAsync();

        Assert.False(result.IsLiveData);
        Assert.Equal(6, result.Items.Count);
        Assert.All(result.Items, item => Assert.False(item.IsLive));
    }

    [Fact]
    public async Task GetMarketTickerAsync_SecondCall_HitsMemoryCacheWithoutExtraHttpCalls()
    {
        var callCount = 0;
        var handler = new StubHttpMessageHandler(_ =>
        {
            Interlocked.Increment(ref callCount);
            return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
        });

        var cacheMock = CreatePassThroughCacheMock();
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        using var httpClient = new HttpClient(handler);
        var service = new MarketTickerService(
            httpClient,
            cacheMock.Object,
            memoryCache,
            NullLogger<MarketTickerService>.Instance);

        await service.GetMarketTickerAsync();
        var callsAfterFirst = callCount;

        await service.GetMarketTickerAsync();

        Assert.Equal(3, callsAfterFirst);
        Assert.Equal(callsAfterFirst, callCount);
    }

    private static Mock<ICacheService> CreatePassThroughCacheMock()
    {
        var mock = new Mock<ICacheService>();
        mock.Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<MarketTickerResponseDto>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns((string _, Func<CancellationToken, Task<MarketTickerResponseDto>> factory, TimeSpan? _, CancellationToken ct) =>
                factory(ct));
        return mock;
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responder(request));
    }
}
