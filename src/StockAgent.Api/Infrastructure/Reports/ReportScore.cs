namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Structured score summary rendered at the top of a research report.
/// </summary>
/// <param name="OverallScore">Overall score from 0 to 100.</param>
/// <param name="RiskLevel">Human-readable risk level.</param>
/// <param name="ValuationView">Short valuation conclusion.</param>
/// <param name="Confidence">Confidence value from 0 to 1 for the report summary.</param>
public sealed record ReportScore(int OverallScore, string RiskLevel, string ValuationView, decimal Confidence);
