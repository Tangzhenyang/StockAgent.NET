using System.Net;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Converts bounded analysis output into Chinese Markdown and HTML research reports.
/// </summary>
public sealed class ReportGenerator
{
    /// <summary>
    /// Generates a readable Chinese report without direct buy/sell instructions.
    /// </summary>
    /// <param name="snapshot">Structured market and company snapshot.</param>
    /// <param name="analysis">Structured AI analysis result.</param>
    /// <param name="evidenceCards">Evidence cards selected for report citations.</param>
    /// <returns>Generated Markdown, HTML, and score payload.</returns>
    public GeneratedReport Generate(MarketDataSnapshot snapshot, AiAnalysisResult analysis, IReadOnlyList<EvidenceCard> evidenceCards)
    {
        var markdown = $"""
        # {snapshot.CompanyName} {snapshot.Ticker} 深度研究报告

        ## 评分结论

        - 综合评分：{analysis.OverallScore}/100
        - 风险等级：{analysis.RiskLevel}
        - 估值判断：{analysis.ValuationView}

        ## 核心摘要

        {analysis.Summary}

        ## 关键假设

        {string.Join(Environment.NewLine, analysis.KeyAssumptions.Select(x => $"- {x}"))}

        ## 来源证据

        {string.Join(Environment.NewLine, evidenceCards.Select(x => $"- {x.Claim}：{x.Snippet}"))}

        ## 风险提示

        本报告仅用于研究辅助，不构成买卖建议。数据和公开材料可能存在延迟、遗漏或解释偏差。
        """;

        var html = $"<article>{WebUtility.HtmlEncode(markdown).Replace("\n", "<br />")}</article>";
        var score = new ReportScore(analysis.OverallScore, analysis.RiskLevel, analysis.ValuationView, 0.72m);

        return new GeneratedReport(markdown, html, score);
    }
}

/// <summary>
/// Generated report payload before persistence.
/// </summary>
/// <param name="Markdown">Markdown report body.</param>
/// <param name="Html">HTML report body.</param>
/// <param name="Score">Structured score summary.</param>
public sealed record GeneratedReport(string Markdown, string Html, ReportScore Score);
