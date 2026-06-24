namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Hard limits that keep each LLM agent input bounded and predictable.
/// 保持每个 LLM Agent 输入受限且可预测的硬限制。
/// </summary>
public sealed class AgentContextBudgetOptions
{
    /// <summary>Maximum evidence cards passed to EvidenceFilingAgent. 传给 EvidenceFilingAgent 的最大证据卡数量。</summary>
    public int MaxEvidenceCardsForEvidenceAgent { get; init; } = 30;

    /// <summary>Maximum snippet length per evidence item. 每条证据摘要的最大字符数。</summary>
    public int MaxEvidenceSnippetCharacters { get; init; } = 400;

    /// <summary>Maximum strengths emitted by MarketFinancialAgent. MarketFinancialAgent 输出的最大优势数量。</summary>
    public int MaxMarketStrengths { get; init; } = 5;

    /// <summary>Maximum risks emitted by MarketFinancialAgent. MarketFinancialAgent 输出的最大风险数量。</summary>
    public int MaxMarketRisks { get; init; } = 5;

    /// <summary>Maximum positive facts emitted by EvidenceFilingAgent. EvidenceFilingAgent 输出的最大正面事实数量。</summary>
    public int MaxEvidencePositiveFacts { get; init; } = 8;

    /// <summary>Maximum negative facts emitted by EvidenceFilingAgent. EvidenceFilingAgent 输出的最大负面事实数量。</summary>
    public int MaxEvidenceNegativeFacts { get; init; } = 8;

    /// <summary>Maximum citations passed into synthesis and review. 传入综合和审核阶段的最大引用数量。</summary>
    public int MaxCitations { get; init; } = 15;

    /// <summary>Maximum final report Markdown characters reviewed by ReviewAgent. ReviewAgent 审核的最终报告最大字符数。</summary>
    public int MaxReviewReportCharacters { get; init; } = 6000;

    /// <summary>Maximum key claims reviewed by ReviewAgent. ReviewAgent 审核的最大关键结论数量。</summary>
    public int MaxReviewKeyClaims { get; init; } = 12;

    /// <summary>Maximum citations reviewed by ReviewAgent. ReviewAgent 审核的最大引用数量。</summary>
    public int MaxReviewCitations { get; init; } = 15;
}
