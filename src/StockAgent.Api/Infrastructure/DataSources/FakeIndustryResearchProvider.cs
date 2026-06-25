using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>Deterministic industry provider for local testing. 用于本地测试的确定性行业数据提供器。</summary>
public sealed class FakeIndustryResearchProvider : IIndustryResearchProvider
{
    /// <inheritdoc />
    public Task<IndustryResearchSnapshot> GetIndustryAsync(
        string ticker,
        string companyName,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        var isStorage = ticker.StartsWith("301308", StringComparison.OrdinalIgnoreCase)
                        || companyName.Contains("江波龙", StringComparison.OrdinalIgnoreCase);
        var industryName = isStorage ? "半导体存储" : "待识别行业";
        IReadOnlyList<string> sectors = isStorage
            ? ["半导体", "存储芯片", "NAND Flash", "DRAM", "嵌入式存储"]
            : ["行业待识别"];
        IReadOnlyList<string> keywords = isStorage
            ? ["存储芯片", "DRAM", "NAND Flash", "存储模组"]
            : [companyName, "行业新闻"];

        return Task.FromResult(new IndustryResearchSnapshot(
            ticker,
            companyName,
            industryName,
            sectors,
            keywords,
            "fake-industry",
            DateTimeOffset.UtcNow,
            [
                new IndustryNewsItem(
                    $"{industryName} 行业景气度跟踪",
                    "https://example.local/industry",
                    "fake",
                    DateTimeOffset.UtcNow.AddDays(-1),
                    $"{industryName} 相关供需、价格和库存变化需要持续跟踪。")
            ]));
    }
}
