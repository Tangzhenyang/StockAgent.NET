namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Structured AI analysis result used by scoring and report generation.
/// </summary>
/// <param name="OverallScore">Overall score from 0 to 100.</param>
/// <param name="RiskLevel">Human-readable risk level.</param>
/// <param name="ValuationView">Short valuation conclusion.</param>
/// <param name="Summary">Concise business and investment summary.</param>
/// <param name="KeyAssumptions">Assumptions used by the generated analysis.</param>
public sealed record AiAnalysisResult(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions);
