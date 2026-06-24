namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Deterministic quality gates for agent outputs before the next LLM stage consumes them.
/// Agent 输出进入下一 LLM 阶段前的确定性质量门禁。
/// </summary>
public static class AgentOutputValidators
{
    /// <summary>Validates market analysis output shape and score range. 校验行情财务分析输出结构和分数范围。</summary>
    public static void ValidateMarket(MarketFinancialAgentOutput output)
    {
        if (output.Score is < 0 or > 100)
        {
            throw new InvalidOperationException("MarketFinancialAgent score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(output.ValuationView))
        {
            throw new InvalidOperationException("MarketFinancialAgent valuation view is required.");
        }
    }

    /// <summary>Validates evidence output citations point to real evidence pack ids. 校验证据输出引用来自真实证据包。</summary>
    public static void ValidateEvidence(EvidenceFilingAgentOutput output, EvidenceFilingAgentInput input)
    {
        var allowedIds = input.EvidencePack.Select(x => x.EvidenceCardId).ToHashSet();
        var invalidCitation = output.Citations.FirstOrDefault(x => !allowedIds.Contains(x.EvidenceCardId));
        if (invalidCitation is not null)
        {
            throw new InvalidOperationException($"EvidenceFilingAgent cited unknown evidence card {invalidCitation.EvidenceCardId}.");
        }
    }

    /// <summary>Validates synthesis produced a usable report and evidence-bound key claims. 校验综合阶段生成可用报告和带证据绑定的关键结论。</summary>
    public static void ValidateSynthesis(SynthesisReportAgentOutput output)
    {
        if (output.OverallScore is < 0 or > 100)
        {
            throw new InvalidOperationException("SynthesisReportAgent score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(output.Markdown))
        {
            throw new InvalidOperationException("SynthesisReportAgent markdown is required.");
        }

        if (output.KeyClaims.Any(x => x.EvidenceCardIds.Count == 0))
        {
            throw new InvalidOperationException("Each key claim must reference at least one evidence card.");
        }
    }
}
