namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Fetches structured market and financial data for a normalized ticker.
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>Gets a deterministic structured snapshot for the requested ticker.</summary>
    Task<MarketDataSnapshot> GetSnapshotAsync(string ticker, CancellationToken cancellationToken);
}
