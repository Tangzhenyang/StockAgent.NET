using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Deterministic market data provider used to keep the MVP workflow testable before real provider selection.
/// </summary>
public sealed class FakeMarketDataProvider : IMarketDataProvider
{
    /// <inheritdoc />
    public Task<MarketDataSnapshot> GetSnapshotAsync(string ticker, CancellationToken cancellationToken)
    {
        var market = ticker.EndsWith(".HK", StringComparison.OrdinalIgnoreCase) ? Market.HongKong : Market.AShare;
        var companyName = market == Market.HongKong ? "腾讯控股" : "示例公司";
        var snapshot = new MarketDataSnapshot(ticker, market, companyName, 320.50m, 3_000_000_000_000m, 18.4m, 8.2m, 24.5m);
        return Task.FromResult(snapshot);
    }
}
