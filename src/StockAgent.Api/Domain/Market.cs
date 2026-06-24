namespace StockAgent.Api.Domain;

/// <summary>
/// Identifies the supported stock markets for first-version research tasks.
/// 标识首个版本研究任务支持的股票市场。
/// </summary>
public enum Market
{
    /// <summary>A-share market, normally using Shanghai or Shenzhen exchange suffixes. A 股市场，通常使用上海或深圳交易所后缀。</summary>
    AShare = 1,

    /// <summary>Hong Kong stock market, normally using the HK suffix. 港股市场，通常使用 HK 后缀。</summary>
    HongKong = 2
}
