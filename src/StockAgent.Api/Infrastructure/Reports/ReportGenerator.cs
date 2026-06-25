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

            ## 投资摘要与核心结论

            - 综合评分：{analysis.OverallScore}/100
            - 风险等级：{analysis.RiskLevel}
            - 估值判断：{analysis.ValuationView}

            {analysis.Summary}

            ## 公司概览

            {snapshot.CompanyName}（{snapshot.Ticker}）当前数据源返回的最新价格为 {snapshot.LastPrice:N2}，市值为 {snapshot.MarketCap:N0}，市盈率为 {snapshot.PeRatio:N1}。本节仅基于当前结构化数据源生成，尚未补充完整业务分部、管理层讨论和行业口径。

            ## 行情与估值分析

            当前 PE 为 {snapshot.PeRatio:N1}，需要结合公司增长质量、行业景气度和利润稳定性综合判断。如果 PE 明显高于稳态盈利能力所能支撑的区间，则后续研究应重点验证盈利增长的持续性和估值消化路径。

            ## 财务质量分析

            数据源返回的最近营收增长率为 {snapshot.RevenueGrowthPercent:N1}%，净利率为 {snapshot.NetMarginPercent:N1}%。在缺少更长周期财务报表、现金流、资产负债表和分部数据时，本报告不会把单期指标推断为长期趋势。

            ## 公告与事件解读

            {string.Join(Environment.NewLine, evidenceCards.Select(x => $"- {x.Claim}：{x.Snippet}"))}

            ## 关键假设

            {string.Join(Environment.NewLine, analysis.KeyAssumptions.Select(x => $"- {x}"))}

            ## 证据不足与无法推断事项

            - 当前报告未覆盖完整三大财务报表、历史估值分位、同业比较和详细行业数据。
            - 对未来增长、利润率维持和估值中枢变化的判断均需要更多公告、定期报告和第三方可复核数据支持。
            - 如果证据卡数量较少，报告结论应视为初步研究框架，而不是完整券商研报级结论。

            ## 后续跟踪指标

            - 后续季度收入增长和订单/业务量变化。
            - 毛利率、净利率和现金流质量是否稳定。
            - 重大公告、分红、回购、资本开支和监管政策变化。
            - 同业估值和盈利预期变化。

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
