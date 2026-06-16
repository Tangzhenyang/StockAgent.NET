namespace StockAgent.Api.Domain;

/// <summary>
/// Identifies the supported stock markets for first-version research tasks.
/// </summary>
public enum Market
{
    /// <summary>A-share market, normally using Shanghai or Shenzhen exchange suffixes.</summary>
    AShare = 1,

    /// <summary>Hong Kong stock market, normally using the HK suffix.</summary>
    HongKong = 2
}
