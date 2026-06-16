using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies market-specific ticker normalization for first-version A-share and Hong Kong symbols.
/// </summary>
public sealed class TickerNormalizerTests
{
    /// <summary>
    /// A 6-digit Shanghai symbol is normalized with the SH suffix.
    /// </summary>
    [Fact]
    public void Normalize_AddsShanghaiSuffix_ForSixHundredPrefix()
    {
        var result = TickerNormalizer.Normalize("600519", null);

        result.Ticker.Should().Be("600519.SH");
        result.Market.Should().Be(Market.AShare);
    }

    /// <summary>
    /// A Hong Kong numeric code is normalized to five digits with HK suffix.
    /// </summary>
    [Fact]
    public void Normalize_PadsHongKongTicker_ToFiveDigits()
    {
        var result = TickerNormalizer.Normalize("700", Market.HongKong);

        result.Ticker.Should().Be("00700.HK");
        result.Market.Should().Be(Market.HongKong);
    }

    /// <summary>
    /// Unsupported ticker input fails before creating a task.
    /// </summary>
    [Fact]
    public void Normalize_RejectsUnsupportedTicker()
    {
        var act = () => TickerNormalizer.Normalize("abc", null);

        act.Should().Throw<ArgumentException>().WithMessage("*Unsupported ticker*");
    }
}
