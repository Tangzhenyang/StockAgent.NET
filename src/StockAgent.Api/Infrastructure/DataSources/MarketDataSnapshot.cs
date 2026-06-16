using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Structured market and financial snapshot used by the research pipeline.
/// </summary>
/// <param name="Ticker">Normalized ticker for the researched company.</param>
/// <param name="Market">Resolved stock market.</param>
/// <param name="CompanyName">Company display name returned by the data provider.</param>
/// <param name="LastPrice">Latest provider-reported stock price.</param>
/// <param name="MarketCap">Latest provider-reported market capitalization.</param>
/// <param name="PeRatio">Latest provider-reported price-to-earnings ratio.</param>
/// <param name="RevenueGrowthPercent">Recent revenue growth percentage.</param>
/// <param name="NetMarginPercent">Recent net margin percentage.</param>
public sealed record MarketDataSnapshot(
    string Ticker,
    Market Market,
    string CompanyName,
    decimal LastPrice,
    decimal MarketCap,
    decimal PeRatio,
    decimal RevenueGrowthPercent,
    decimal NetMarginPercent);
