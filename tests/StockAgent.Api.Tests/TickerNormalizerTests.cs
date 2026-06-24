using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies market-specific ticker normalization for first-version A-share and Hong Kong symbols.
/// 验证首个版本中 A 股和港股代码的市场特定规范化。
/// </summary>
public sealed class TickerNormalizerTests
{
    /// <summary>
    /// A 6-digit Shanghai symbol is normalized with the SH suffix.
    /// 6 位上海股票代码会被规范化为 SH 后缀。
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
    /// 港股数字代码会被规范化为 5 位并追加 HK 后缀。
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
    /// 不受支持的股票代码输入会在创建任务前失败。
    /// </summary>
    [Fact]
    public void Normalize_RejectsUnsupportedTicker()
    {
        var act = () => TickerNormalizer.Normalize("abc", null);

        act.Should().Throw<ArgumentException>().WithMessage("*Unsupported ticker*");
    }
}
