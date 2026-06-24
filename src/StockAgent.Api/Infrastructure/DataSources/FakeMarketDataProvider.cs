using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Deterministic market data provider used to keep the MVP workflow testable before real provider selection.
/// 用于在选择真实提供程序之前保持 MVP 工作流可测试的确定性市场数据提供器。
/// </summary>
public sealed class FakeMarketDataProvider : IMarketDataProvider
{
    /// <inheritdoc />
    public Task<MarketDataSnapshot> GetSnapshotAsync(
        string ticker,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        var market = ticker.EndsWith(".HK", StringComparison.OrdinalIgnoreCase) ? Market.HongKong : Market.AShare;
        var companyName = market == Market.HongKong ? "腾讯控股" : "示例公司";
        var snapshot = new MarketDataSnapshot(ticker, market, companyName, 320.50m, 3_000_000_000_000m, 18.4m, 8.2m, 24.5m);
        return Task.FromResult(snapshot);
    }
}
