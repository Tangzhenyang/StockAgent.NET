namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Structured AI analysis result used by scoring and report generation.
/// 用于评分和报告生成的结构化 AI 分析结果。
/// </summary>
/// <param name="OverallScore">Overall score from 0 to 100. 0 到 100 的综合评分。</param>
/// <param name="RiskLevel">Human-readable risk level. 人类可读的风险等级。</param>
/// <param name="ValuationView">Short valuation conclusion. 简短的估值结论。</param>
/// <param name="Summary">Concise business and investment summary. 简洁的业务与投资摘要。</param>
/// <param name="KeyAssumptions">Assumptions used by the generated analysis. 生成分析所使用的假设。</param>
/// <param name="ReportMarkdown">LLM-generated Markdown report body when available. 可用时由大模型生成的 Markdown 报告正文。</param>
/// <param name="AgentTraces">Human-readable agent execution summaries. 人类可读的 Agent 执行摘要。</param>
public sealed record AiAnalysisResult(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions,
    string? ReportMarkdown = null,
    IReadOnlyList<string>? AgentTraces = null);
