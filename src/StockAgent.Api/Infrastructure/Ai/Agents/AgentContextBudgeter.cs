using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Builds bounded agent inputs so raw documents and long prior outputs never flow directly into LLM calls.
/// 构建受限 Agent 输入，避免原始文档和过长中间结果直接进入 LLM 调用。
/// </summary>
public sealed class AgentContextBudgeter(AgentContextBudgetOptions options)
{
    /// <summary>Builds a compact market agent input. 构建紧凑的行情财务 Agent 输入。</summary>
    public MarketFinancialAgentInput BuildMarketInput(MarketDataSnapshot snapshot, string language)
    {
        return new MarketFinancialAgentInput(snapshot, null, language);
    }

    /// <summary>Builds a bounded evidence pack for EvidenceFilingAgent. 为 EvidenceFilingAgent 构建受限证据包。</summary>
    public EvidenceFilingAgentInput BuildEvidenceInput(
        MarketDataSnapshot snapshot,
        IReadOnlyList<EvidenceCard> evidenceCards,
        string language)
    {
        var pack = evidenceCards
            .OrderByDescending(x => x.Relevance)
            .ThenByDescending(x => x.Confidence)
            .Take(options.MaxEvidenceCardsForEvidenceAgent)
            .Select(x => new EvidencePackItem(
                x.Id,
                x.Claim,
                Truncate(x.Snippet, options.MaxEvidenceSnippetCharacters),
                x.ReportSection,
                x.Relevance,
                x.Confidence,
                x.SourceDate))
            .ToList();

        return new EvidenceFilingAgentInput(snapshot, pack, language);
    }

    /// <summary>Builds a bounded synthesis input from validated agent summaries. 从已校验 Agent 摘要构建受限综合输入。</summary>
    public SynthesisReportAgentInput BuildSynthesisInput(
        MarketDataSnapshot snapshot,
        MarketFinancialAgentOutput market,
        EvidenceFilingAgentOutput evidence,
        IndustryResearchAgentOutput? industry,
        string language)
    {
        var boundedMarket = market with
        {
            Strengths = market.Strengths.Take(options.MaxMarketStrengths).ToList(),
            Risks = market.Risks.Take(options.MaxMarketRisks).ToList()
        };
        var boundedEvidence = evidence with
        {
            PositiveFacts = evidence.PositiveFacts.Take(options.MaxEvidencePositiveFacts).ToList(),
            NegativeFacts = evidence.NegativeFacts.Take(options.MaxEvidenceNegativeFacts).ToList(),
            Citations = evidence.Citations.Take(options.MaxCitations).ToList()
        };

        var boundedIndustry = industry is null
            ? null
            : industry with
            {
                Opportunities = industry.Opportunities.Take(6).ToList(),
                Risks = industry.Risks.Take(6).ToList(),
                NewsHighlights = industry.NewsHighlights.Take(8).ToList(),
                FollowUpQuestions = industry.FollowUpQuestions.Take(6).ToList()
            };

        return new SynthesisReportAgentInput(snapshot, boundedMarket, boundedEvidence, boundedIndustry, language);
    }

    /// <summary>Builds a bounded review input from report key claims and citations only. 仅用报告关键结论和引用构建受限审核输入。</summary>
    public ReviewAgentInput BuildReviewInput(
        MarketDataSnapshot snapshot,
        SynthesisReportAgentOutput draft,
        EvidenceFilingAgentOutput evidence,
        string language)
    {
        return new ReviewAgentInput(
            snapshot,
            Truncate(draft.Markdown, options.MaxReviewReportCharacters),
            draft.KeyClaims.Take(options.MaxReviewKeyClaims).ToList(),
            evidence.Citations.Take(options.MaxReviewCitations).ToList(),
            language);
    }

    private static string Truncate(string value, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            return string.Empty;
        }

        return value.Length <= maxCharacters ? value : value[..maxCharacters];
    }
}
