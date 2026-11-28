namespace NewsHub.Application.Common.Interfaces;

public sealed record MarketTickerItemDto(
    string Symbol,
    string Value,
    string Change,
    bool IsPositive,
    bool IsLive);

public sealed record MarketTickerResponseDto(
    IReadOnlyList<MarketTickerItemDto> Items,
    DateTimeOffset UpdatedAt,
    bool IsLiveData);

public interface IMarketTickerService
{
    Task<MarketTickerResponseDto> GetMarketTickerAsync(CancellationToken cancellationToken = default);
}
