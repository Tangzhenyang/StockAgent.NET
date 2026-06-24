using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Reports;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies report generation with LLM-generated multi-agent Markdown.
/// 验证使用多 Agent 大模型 Markdown 生成报告。
/// </summary>
public sealed class ReportGeneratorMultiAgentTests
{
    /// <summary>
    /// LLM-generated Markdown is used when present.
    /// 存在大模型 Markdown 时优先使用它。
    /// </summary>
    [Fact]
    public void Generate_UsesMultiAgentMarkdownWhenPresent()
    {
        var generator = new ReportGenerator();
        var snapshot = new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1m, 1m, 1m, 1m, 1m);
        var analysis = new AiAnalysisResult(
            68,
            "中等",
            "估值偏高",
            "摘要",
            ["收入增长延续"],
            "# 自定义多 Agent 报告\n\n## 评分结论\n综合评分：68/100");

        var report = generator.Generate(snapshot, analysis, []);

        report.Markdown.Should().StartWith("# 自定义多 Agent 报告");
        report.Html.Should().Contain("自定义多 Agent 报告");
        report.Score.OverallScore.Should().Be(68);
    }
}
