using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NewsHub.Application.Common.Interfaces;

namespace NewsHub.Infrastructure.Services;

/// <summary>
/// Tổng hợp chỉ số thị trường Việt Nam (VN-INDEX, VN30, HNX-INDEX, UPCOM, USD/VND, SJC 9999)
/// từ các nguồn công khai với Cache-Aside 2 tầng (Redis 10 phút + MemoryCache dự phòng) và fail-open fallback.
/// </summary>
public sealed class MarketTickerService(
    HttpClient httpClient,
    ICacheService cacheService,
    IMemoryCache memoryCache,
    ILogger<MarketTickerService> logger) : IMarketTickerService
{
    private const string CacheKey = "public:market-ticker:v1";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private const string VnDirectUrl =
        "https://finfo-api.vndirect.com.vn/v4/vnmarket_prices?sort=date&q=code:VNINDEX,VN30,HNX,UPCOM&size=4";
    private const string OpenErUsdUrl = "https://open.er-api.com/v6/latest/USD";
    private const string SjcGoldUrl = "https://sjc.com.vn/GoldPrice/Services/PriceService.ashx";

    private static readonly IReadOnlyList<MarketTickerItemDto> FallbackItems =
    [
        new("VN-INDEX", "1,286.42", "+0.48%", true, false),
        new("VN30", "1,342.15", "+0.62%", true, false),
        new("HNX-INDEX", "234.18", "-0.19%", false, false),
        new("UPCOM", "93.45", "+0.14%", true, false),
        new("USD/VND", "25,380", "+0.05%", true, false),
        new("SJC 9999", "84.50tr", "+0.36%", true, false),
    ];

    public async Task<MarketTickerResponseDto> GetMarketTickerAsync(CancellationToken cancellationToken = default)
    {
        if (memoryCache.TryGetValue(CacheKey, out MarketTickerResponseDto? localCached) && localCached is not null)
        {
            return localCached;
        }

        var result = await cacheService.GetOrCreateAsync(
            CacheKey,
            FetchLiveOrFallbackAsync,
            CacheTtl,
            cancellationToken);

        memoryCache.Set(CacheKey, result, CacheTtl);
        return result;
    }

    private async Task<MarketTickerResponseDto> FetchLiveOrFallbackAsync(CancellationToken cancellationToken)
    {
        var vnTask = FetchVnStockIndicesAsync(cancellationToken);
        var fxTask = FetchUsdVndAsync(cancellationToken);
        var goldTask = FetchSjcGoldAsync(cancellationToken);

        await Task.WhenAll(vnTask, fxTask, goldTask);

        var liveMap = new Dictionary<string, MarketTickerItemDto>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in vnTask.Result)
        {
            liveMap[item.Symbol] = item;
        }

        if (fxTask.Result is not null)
        {
            liveMap[fxTask.Result.Symbol] = fxTask.Result;
        }

        if (goldTask.Result is not null)
        {
            liveMap[goldTask.Result.Symbol] = goldTask.Result;
        }

        var merged = FallbackItems
            .Select(fallback => liveMap.TryGetValue(fallback.Symbol, out var live) ? live : fallback)
            .ToList();

        var isAnyLive = merged.Any(x => x.IsLive);
        return new MarketTickerResponseDto(merged, DateTimeOffset.UtcNow, isAnyLive);
    }

    private async Task<List<MarketTickerItemDto>> FetchVnStockIndicesAsync(CancellationToken cancellationToken)
    {
        var results = new List<MarketTickerItemDto>();
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            using var response = await httpClient.GetAsync(VnDirectUrl, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return results;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

            if (!doc.RootElement.TryGetProperty("data", out var dataElement) ||
                dataElement.ValueKind != JsonValueKind.Array)
            {
                return results;
            }

            foreach (var row in dataElement.EnumerateArray())
            {
                if (!row.TryGetProperty("code", out var codeEl)) continue;
                var code = codeEl.GetString()?.ToUpperInvariant();
                var symbol = code switch
                {
                    "VNINDEX" => "VN-INDEX",
                    "VN30" => "VN30",
                    "HNX" => "HNX-INDEX",
                    "UPCOM" => "UPCOM",
                    _ => null,
                };
                if (symbol is null) continue;

                var close = TryReadDecimal(row, "close");
                var pctChange = TryReadDecimal(row, "pctChange");
                if (close is null || close <= 0) continue;

                var pct = pctChange ?? 0m;
                var sign = pct >= 0 ? "+" : string.Empty;
                results.Add(new MarketTickerItemDto(
                    symbol,
                    close.Value.ToString("N2", CultureInfo.InvariantCulture),
                    $"{sign}{pct.ToString("0.00", CultureInfo.InvariantCulture)}%",
                    pct >= 0,
                    true));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Không thể lấy chỉ số chứng khoán VNDirect, sử dụng dữ liệu dự phòng.");
        }

        return results;
    }

    private async Task<MarketTickerItemDto?> FetchUsdVndAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            using var response = await httpClient.GetAsync(OpenErUsdUrl, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

            if (!doc.RootElement.TryGetProperty("rates", out var ratesEl))
            {
                return null;
            }

            var vnd = TryReadDecimal(ratesEl, "VND");
            if (vnd is null || vnd <= 0)
            {
                return null;
            }

            return new MarketTickerItemDto(
                "USD/VND",
                Math.Round(vnd.Value, 0).ToString("N0", CultureInfo.InvariantCulture),
                "Tỷ giá",
                true,
                true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Không thể lấy tỷ giá USD/VND từ Open ER API, sử dụng dữ liệu dự phòng.");
            return null;
        }
    }

    private async Task<MarketTickerItemDto?> FetchSjcGoldAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(4));

            using var response = await httpClient.GetAsync(SjcGoldUrl, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cts.Token);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cts.Token);

            if (!doc.RootElement.TryGetProperty("data", out var dataEl) ||
                dataEl.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            foreach (var row in dataEl.EnumerateArray())
            {
                var typeName = row.TryGetProperty("TypeName", out var typeEl) ? typeEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(typeName) ||
                    !typeName.Contains("SJC", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var sellValue = TryReadDecimal(row, "SellValue");
                if (sellValue is null || sellValue <= 0) continue;

                var millions = sellValue.Value >= 1_000_000m
                    ? sellValue.Value / 1_000_000m
                    : sellValue.Value / 1_000m;

                var buyValue = TryReadDecimal(row, "BuyValue");
                var spreadLabel = buyValue is > 0
                    ? $"Mua {(buyValue.Value >= 1_000_000m ? buyValue.Value / 1_000_000m : buyValue.Value / 1_000m).ToString("0.0", CultureInfo.InvariantCulture)}tr"
                    : "Bán ra";

                return new MarketTickerItemDto(
                    "SJC 9999",
                    $"{millions.ToString("0.00", CultureInfo.InvariantCulture)}tr",
                    spreadLabel,
                    true,
                    true);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Không thể lấy giá vàng SJC, sử dụng dữ liệu dự phòng.");
        }

        return null;
    }

    private static decimal? TryReadDecimal(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var prop))
        {
            return null;
        }

        if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var dec))
        {
            return dec;
        }

        if (prop.ValueKind == JsonValueKind.String &&
            decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        return null;
    }
}
