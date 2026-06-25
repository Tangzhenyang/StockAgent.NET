using System.Diagnostics;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Fixed-flow multi-agent analysis service backed by Semantic Kernel chat completion.
/// 基于 Semantic Kernel 聊天补全的固定流程多 Agent 分析服务。
/// </summary>
public sealed class SemanticKernelResearchAnalysisService(
    IModelChatClient chatClient,
    AgentContextBudgeter contextBudgeter,
    StockAgentDbContext db,
    ILogger<SemanticKernelResearchAnalysisService> logger) : IResearchAnalysisService
{
    /// <inheritdoc />
    public async Task<AiAnalysisResult> AnalyzeAsync(
        Guid researchTaskId,
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        ModelRuntimeSettings modelSettings,
        string language,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting fixed-flow multi-agent analysis for {Ticker} with {EvidenceCount} evidence cards.",
            marketData.Ticker,
            evidenceCards.Count);

        var marketAgent = new MarketFinancialAgent(chatClient, modelSettings);
        var evidenceAgent = new EvidenceFilingAgent(chatClient, modelSettings);
        var marketInput = contextBudgeter.BuildMarketInput(marketData, language);
        var evidenceInput = contextBudgeter.BuildEvidenceInput(marketData, evidenceCards, language);
        var marketTask = RunMeasuredAsync(marketAgent, marketInput, modelSettings, cancellationToken);
        var evidenceTask = RunMeasuredAsync(evidenceAgent, evidenceInput, modelSettings, cancellationToken);
        await Task.WhenAll(marketTask, evidenceTask);
        var evidenceOutput = SanitizeEvidenceOutput(evidenceTask.Result.Output, evidenceInput);
        AgentOutputValidators.ValidateMarket(marketTask.Result.Output);
        AgentOutputValidators.ValidateEvidence(evidenceOutput, evidenceInput);
        await SaveInvocationsAsync(researchTaskId, [marketTask.Result.Invocation, evidenceTask.Result.Invocation], cancellationToken);

        var synthesisAgent = new SynthesisReportAgent(chatClient, modelSettings);
        var synthesisInput = contextBudgeter.BuildSynthesisInput(
            marketData,
            marketTask.Result.Output,
            evidenceOutput,
            language);
        var synthesisRun = await RunMeasuredAsync(synthesisAgent, synthesisInput, modelSettings, cancellationToken);
        var synthesisOutput = SanitizeSynthesisOutput(synthesisRun.Output, evidenceOutput);
        AgentOutputValidators.ValidateSynthesis(synthesisOutput);
        await SaveInvocationsAsync(researchTaskId, [synthesisRun.Invocation], cancellationToken);

        var reviewAgent = new ReviewAgent(chatClient, modelSettings);
        var reviewInput = contextBudgeter.BuildReviewInput(
            marketData,
            synthesisOutput,
            evidenceOutput,
            language);
        var reviewRun = await RunMeasuredAsync(reviewAgent, reviewInput, modelSettings, cancellationToken);
        await SaveInvocationsAsync(researchTaskId, [reviewRun.Invocation], cancellationToken);

        if (!reviewRun.Output.Approved)
        {
            var limitedMarkdown = BuildLimitedReportMarkdown(synthesisOutput, reviewRun.Output, evidenceOutput);
            return new AiAnalysisResult(
                synthesisOutput.OverallScore,
                synthesisOutput.RiskLevel,
                synthesisOutput.ValuationView,
                synthesisOutput.Summary,
                [
                    ..synthesisOutput.KeyAssumptions,
                    ..reviewRun.Output.Issues.Select(x => $"待验证：{x}")
                ],
                limitedMarkdown,
                [
                    "MarketFinancialAgent:Succeeded",
                    "EvidenceFilingAgent:Succeeded",
                    "SynthesisReportAgent:Succeeded",
                    "ReviewAgent:LimitedReport"
                ]);
        }

        return new AiAnalysisResult(
            synthesisOutput.OverallScore,
            synthesisOutput.RiskLevel,
            synthesisOutput.ValuationView,
            synthesisOutput.Summary,
            synthesisOutput.KeyAssumptions,
            synthesisOutput.Markdown,
            [
                "MarketFinancialAgent:Succeeded",
                "EvidenceFilingAgent:Succeeded",
                "SynthesisReportAgent:Succeeded",
                "ReviewAgent:Approved"
            ]);
    }

    private static EvidenceFilingAgentOutput SanitizeEvidenceOutput(
        EvidenceFilingAgentOutput output,
        EvidenceFilingAgentInput input)
    {
        var allowedEvidenceIds = input.EvidencePack.Select(x => x.EvidenceCardId).ToHashSet();
        var citations = (output.Citations ?? [])
            .Where(x => x.EvidenceCardId != Guid.Empty && allowedEvidenceIds.Contains(x.EvidenceCardId))
            .ToList();

        return output with { Citations = citations };
    }

    private static SynthesisReportAgentOutput SanitizeSynthesisOutput(
        SynthesisReportAgentOutput output,
        EvidenceFilingAgentOutput evidenceOutput)
    {
        var allowedEvidenceIds = (evidenceOutput.Citations ?? [])
            .Select(x => x.EvidenceCardId)
            .Where(x => x != Guid.Empty)
            .ToHashSet();
        var keyClaims = (output.KeyClaims ?? [])
            .Select(x => x with
            {
                EvidenceCardIds = (x.EvidenceCardIds ?? [])
                    .Where(allowedEvidenceIds.Contains)
                    .Distinct()
                    .ToList()
            })
            .Where(x => x.EvidenceCardIds.Count > 0)
            .ToList();

        return output with { KeyClaims = keyClaims };
    }

    private static string BuildLimitedReportMarkdown(
        SynthesisReportAgentOutput synthesisOutput,
        ReviewAgentOutput reviewOutput,
        EvidenceFilingAgentOutput evidenceOutput)
    {
        var issues = reviewOutput.Issues.Count == 0
            ? "- 审核 Agent 未返回具体问题，但未批准该报告。"
            : string.Join(Environment.NewLine, reviewOutput.Issues.Select(x => $"- {x}"));
        var assumptions = synthesisOutput.KeyAssumptions.Count == 0
            ? "- 暂无可确认的关键假设。"
            : string.Join(Environment.NewLine, synthesisOutput.KeyAssumptions.Select(x => $"- {x}"));
        var citations = evidenceOutput.Citations.Count == 0
            ? "- 当前没有可绑定到报告结论的有效证据引用。"
            : string.Join(Environment.NewLine, evidenceOutput.Citations.Select(x => $"- {x.Title}：{x.Snippet}"));

        return $"""
        {synthesisOutput.Markdown}

        ## 证据不足与无法推断事项

        本报告已生成为受限研究报告：系统保留已有行情、财务和公开证据分析，但最终审核发现部分结论仍缺少充分证据支撑。因此，下列内容不应被视为已被公告、财务数据或公开证据完全验证。

        ### 审核发现的问题

        {issues}

        ### 需要继续验证的关键假设

        {assumptions}

        ### 当前可用证据边界

        {citations}

        ### 使用限制

        - 对缺少明确公告、财务明细或来源引用的判断，仅作为研究假设。
        - 后续应优先补充公告原文、定期报告、业绩说明材料及可复核财务数据。
        - 本报告仅用于研究辅助，不构成买卖建议。
        """;
    }

    private static async Task<AgentRunResult<TOutput>> RunMeasuredAsync<TInput, TOutput>(
        IResearchAgent<TInput, TOutput> agent,
        TInput input,
        ModelRuntimeSettings modelSettings,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        var output = await agent.RunAsync(input, cancellationToken);
        var durationMs = (long)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
        return new AgentRunResult<TOutput>(
            output,
            new ModelInvocation
            {
                StepName = agent.Name,
                Provider = modelSettings.Provider,
                ModelName = modelSettings.Model,
                DurationMs = durationMs,
                Status = "Succeeded"
            });
    }

    private async Task SaveInvocationsAsync(
        Guid researchTaskId,
        IReadOnlyList<ModelInvocation> invocations,
        CancellationToken cancellationToken)
    {
        foreach (var invocation in invocations)
        {
            invocation.ResearchTaskId = researchTaskId;
        }

        db.ModelInvocations.AddRange(invocations);
        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed record AgentRunResult<TOutput>(TOutput Output, ModelInvocation Invocation);
}
