using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Input for market and financial analysis. 行情与财务分析输入。</summary>
public sealed record MarketFinancialAgentInput(
    MarketDataSnapshot Snapshot,
    FinancialTrendSummary? TrendSummary,
    string Language);

/// <summary>Optional compact financial trend summary. 可选的紧凑财务趋势摘要。</summary>
public sealed record FinancialTrendSummary(
    IReadOnlyList<string> RevenueTrendFacts,
    IReadOnlyList<string> MarginTrendFacts,
    IReadOnlyList<string> PeerComparisonFacts);

/// <summary>Structured output from market and financial analysis. 行情与财务分析结构化输出。</summary>
public sealed record MarketFinancialAgentOutput(
    int Score,
    string ValuationView,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> FollowUpQuestions);

/// <summary>Input for public evidence and filing analysis. 公告与公开证据分析输入。</summary>
public sealed record EvidenceFilingAgentInput(
    MarketDataSnapshot Snapshot,
    IReadOnlyList<EvidencePackItem> EvidencePack,
    string Language);

/// <summary>Bounded citation-ready evidence item passed to LLM agents. 传给 LLM Agent 的受限可引用证据项。</summary>
public sealed record EvidencePackItem(
    Guid EvidenceCardId,
    string Title,
    string Snippet,
    string ReportSection,
    decimal Relevance,
    decimal Confidence,
    DateTimeOffset? SourceDate);

/// <summary>One citation extracted from an evidence card. 从证据卡提取的一条引用。</summary>
public sealed record EvidenceCitation(Guid EvidenceCardId, string Title, string Snippet, DateTimeOffset? SourceDate);

/// <summary>Structured output from public evidence and filing analysis. 公告与公开证据分析结构化输出。</summary>
public sealed record EvidenceFilingAgentOutput(
    IReadOnlyList<string> PositiveFacts,
    IReadOnlyList<string> NegativeFacts,
    IReadOnlyList<string> Uncertainties,
    IReadOnlyList<EvidenceCitation> Citations);

/// <summary>Input for industry and sector analysis. 行业与赛道分析输入。</summary>
public sealed record IndustryResearchAgentInput(
    MarketDataSnapshot Snapshot,
    IndustryResearchSnapshot Industry,
    string Language);

/// <summary>Structured output from industry and sector analysis. 行业与赛道分析结构化输出。</summary>
public sealed record IndustryResearchAgentOutput(
    string IndustryView,
    IReadOnlyList<string> Opportunities,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> NewsHighlights,
    IReadOnlyList<string> FollowUpQuestions);

/// <summary>Input for report synthesis. 报告综合输入。</summary>
public sealed record SynthesisReportAgentInput(
    MarketDataSnapshot Snapshot,
    MarketFinancialAgentOutput MarketAnalysis,
    EvidenceFilingAgentOutput EvidenceAnalysis,
    IndustryResearchAgentOutput? IndustryAnalysis,
    string Language);

/// <summary>Structured final report draft from synthesis. 综合 Agent 生成的结构化报告草稿。</summary>
public sealed record SynthesisReportAgentOutput(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions,
    IReadOnlyList<ReportKeyClaim> KeyClaims,
    string Markdown);

/// <summary>Evidence-bound key claim produced with the final report. 最终报告中带证据绑定的关键结论。</summary>
public sealed record ReportKeyClaim(string Claim, IReadOnlyList<Guid> EvidenceCardIds);

/// <summary>Input for final quality review. 最终质量审核输入。</summary>
public sealed record ReviewAgentInput(
    MarketDataSnapshot Snapshot,
    string ReportMarkdown,
    IReadOnlyList<ReportKeyClaim> KeyClaims,
    IReadOnlyList<EvidenceCitation> Citations,
    string Language);

/// <summary>Review result for evidence binding and overclaim checks. 证据绑定和过度结论检查结果。</summary>
public sealed record ReviewAgentOutput(bool Approved, IReadOnlyList<string> Issues, string RevisionInstruction);
