using System.Net;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Converts bounded analysis output into Chinese Markdown and HTML research reports.
/// 将受限分析结果转换为中文 Markdown 和 HTML 研究报告。
/// </summary>
public sealed class ReportGenerator
{
    /// <summary>
    /// Generates a readable Chinese report without direct buy/sell instructions.
    /// 生成可读的中文报告，不包含直接买卖指令。
    /// </summary>
    /// <param name="snapshot">Structured market and company snapshot. 结构化的市场和公司快照。</param>
    /// <param name="analysis">Structured AI analysis result. 结构化 AI 分析结果。</param>
    /// <param name="evidenceCards">Evidence cards selected for report citations. 为报告引用选择的证据卡。</param>
    /// <returns>Generated Markdown, HTML, and score payload. 生成的 Markdown、HTML 和评分载荷。</returns>
    public GeneratedReport Generate(MarketDataSnapshot snapshot, AiAnalysisResult analysis, IReadOnlyList<EvidenceCard> evidenceCards)
    {
        var markdown = string.IsNullOrWhiteSpace(analysis.ReportMarkdown)
            ? $"""
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
            """
            : analysis.ReportMarkdown;

        var html = $"<article>{WebUtility.HtmlEncode(markdown).Replace("\n", "<br />")}</article>";
        var score = new ReportScore(analysis.OverallScore, analysis.RiskLevel, analysis.ValuationView, 0.72m);

        return new GeneratedReport(markdown, html, score);
    }
}

/// <summary>
/// Generated report payload before persistence.
/// 持久化前生成的报告载荷。
/// </summary>
/// <param name="Markdown">Markdown report body. Markdown 报告正文。</param>
/// <param name="Html">HTML report body. HTML 报告正文。</param>
/// <param name="Score">Structured score summary. 结构化评分摘要。</param>
public sealed record GeneratedReport(string Markdown, string Html, ReportScore Score);
