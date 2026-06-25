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
        AgentOutputValidators.ValidateSynthesis(synthesisRun.Output);
        await SaveInvocationsAsync(researchTaskId, [synthesisRun.Invocation], cancellationToken);

        var reviewAgent = new ReviewAgent(chatClient, modelSettings);
        var reviewInput = contextBudgeter.BuildReviewInput(
            marketData,
            synthesisRun.Output,
            evidenceOutput,
            language);
        var reviewRun = await RunMeasuredAsync(reviewAgent, reviewInput, modelSettings, cancellationToken);
        await SaveInvocationsAsync(researchTaskId, [reviewRun.Invocation], cancellationToken);

        if (!reviewRun.Output.Approved)
        {
            throw new InvalidOperationException($"Report review failed: {string.Join("; ", reviewRun.Output.Issues)}");
        }

        return new AiAnalysisResult(
            synthesisRun.Output.OverallScore,
            synthesisRun.Output.RiskLevel,
            synthesisRun.Output.ValuationView,
            synthesisRun.Output.Summary,
            synthesisRun.Output.KeyAssumptions,
            synthesisRun.Output.Markdown,
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
