using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Fetches structured market and financial data for a normalized ticker.
/// 获取规范化股票代码对应的结构化市场和财务数据。
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>Gets a structured snapshot for the requested ticker using the user's data source settings. 使用用户数据源设置获取请求股票代码的结构化快照。</summary>
    Task<MarketDataSnapshot> GetSnapshotAsync(
        string ticker,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken);
}
