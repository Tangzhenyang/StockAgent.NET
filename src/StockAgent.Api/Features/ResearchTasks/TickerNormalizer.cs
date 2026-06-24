using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Normalizes user-entered A-share and Hong Kong stock tickers into canonical suffix format.
/// 将用户输入的 A 股和港股股票代码规范化为标准后缀格式。
/// </summary>
public static class TickerNormalizer
{
    /// <summary>
    /// Normalizes a ticker and infers the market when possible.
    /// 规范化股票代码，并在可能时推断市场。
    /// </summary>
    /// <param name="input">User-entered ticker such as 600519, 600519.SH, 700, or 00700.HK. 用户输入的股票代码，例如 600519、600519.SH、700 或 00700.HK。</param>
    /// <param name="marketHint">Optional market selected by the user. 用户选择的可选市场。</param>
    /// <returns>Canonical ticker and market. 规范化后的股票代码和市场。</returns>
    /// <exception cref="ArgumentException">Thrown when the ticker is unsupported or ambiguous. 当股票代码不受支持或存在歧义时抛出。</exception>
    public static NormalizedTicker Normalize(string input, Market? marketHint)
    {
        var trimmed = input.Trim().ToUpperInvariant();

        if (trimmed.EndsWith(".SH", StringComparison.Ordinal) || trimmed.EndsWith(".SZ", StringComparison.Ordinal))
        {
            return new NormalizedTicker(trimmed, Market.AShare);
        }

        if (trimmed.EndsWith(".HK", StringComparison.Ordinal))
        {
            var code = trimmed[..^3].PadLeft(5, '0');
            return new NormalizedTicker($"{code}.HK", Market.HongKong);
        }

        if (trimmed.Length == 6 && trimmed.All(char.IsDigit))
        {
            var suffix = trimmed.StartsWith('6') ? "SH" : "SZ";
            return new NormalizedTicker($"{trimmed}.{suffix}", Market.AShare);
        }

        if (marketHint == Market.HongKong && trimmed.Length <= 5 && trimmed.All(char.IsDigit))
        {
            return new NormalizedTicker($"{trimmed.PadLeft(5, '0')}.HK", Market.HongKong);
        }

        throw new ArgumentException($"Unsupported ticker input: {input}", nameof(input));
    }
}

/// <summary>
/// Canonical ticker value returned by the ticker normalizer.
/// 股票代码归一化器返回的规范化股票值。
/// </summary>
/// <param name="Ticker">Normalized ticker symbol. 规范化后的股票代码。</param>
/// <param name="Market">Resolved market. 已解析的市场。</param>
public sealed record NormalizedTicker(string Ticker, Market Market);
