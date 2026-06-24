namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Structured score summary rendered at the top of a research report.
/// 显示在研究报告顶部的结构化评分摘要。
/// </summary>
/// <param name="OverallScore">Overall score from 0 to 100. 0 到 100 的综合评分。</param>
/// <param name="RiskLevel">Human-readable risk level. 人类可读的风险等级。</param>
/// <param name="ValuationView">Short valuation conclusion. 简短的估值结论。</param>
/// <param name="Confidence">Confidence value from 0 to 1 for the report summary. 报告摘要 0 到 1 的置信度值。</param>
public sealed record ReportScore(int OverallScore, string RiskLevel, string ValuationView, decimal Confidence);
