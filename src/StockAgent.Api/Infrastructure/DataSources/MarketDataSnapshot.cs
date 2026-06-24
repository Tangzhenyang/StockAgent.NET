using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Structured market and financial snapshot used by the research pipeline.
/// 研究管道使用的结构化市场和财务快照。
/// </summary>
/// <param name="Ticker">Normalized ticker for the researched company. 被研究公司对应的规范化股票代码。</param>
/// <param name="Market">Resolved stock market. 已解析的股票市场。</param>
/// <param name="CompanyName">Company display name returned by the data provider. 数据提供器返回的公司显示名称。</param>
/// <param name="LastPrice">Latest provider-reported stock price. 提供器报告的最新股价。</param>
/// <param name="MarketCap">Latest provider-reported market capitalization. 提供器报告的最新市值。</param>
/// <param name="PeRatio">Latest provider-reported price-to-earnings ratio. 提供器报告的最新市盈率。</param>
/// <param name="RevenueGrowthPercent">Recent revenue growth percentage. 最近的营收增长百分比。</param>
/// <param name="NetMarginPercent">Recent net margin percentage. 最近的净利率百分比。</param>
public sealed record MarketDataSnapshot(
    string Ticker,
    Market Market,
    string CompanyName,
    decimal LastPrice,
    decimal MarketCap,
    decimal PeRatio,
    decimal RevenueGrowthPercent,
    decimal NetMarginPercent);
