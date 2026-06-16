using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Normalizes user-entered A-share and Hong Kong stock tickers into canonical suffix format.
/// </summary>
public static class TickerNormalizer
{
    /// <summary>
    /// Normalizes a ticker and infers the market when possible.
    /// </summary>
    /// <param name="input">User-entered ticker such as 600519, 600519.SH, 700, or 00700.HK.</param>
    /// <param name="marketHint">Optional market selected by the user.</param>
    /// <returns>Canonical ticker and market.</returns>
    /// <exception cref="ArgumentException">Thrown when the ticker is unsupported or ambiguous.</exception>
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
/// </summary>
/// <param name="Ticker">Normalized ticker symbol.</param>
/// <param name="Market">Resolved market.</param>
public sealed record NormalizedTicker(string Ticker, Market Market);
