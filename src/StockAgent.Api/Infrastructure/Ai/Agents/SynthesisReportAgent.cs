using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Synthesizes agent outputs into a report draft. 将多个 Agent 输出综合为报告草稿。</summary>
public sealed class SynthesisReportAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<SynthesisReportAgentInput, SynthesisReportAgentOutput>(
        chatClient,
        modelSettings ?? TestSettings)
{
    private static readonly ModelRuntimeSettings TestSettings = new(
        "OpenAICompatible",
        "https://example.test/v1",
        "test-model",
        "test-key");

    /// <inheritdoc />
    public override string Name => "SynthesisReportAgent";

    /// <inheritdoc />
    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究报告综合 Agent。
        只能基于 MarketFinancialAgent 和 EvidenceFilingAgent 的输出生成结论。
        不得创造新事实。所有关键结论必须输出到 keyClaims，并绑定 evidenceCardIds。
        如果某个结论无法绑定至少一个 evidenceCardId，不要把它放入 keyClaims。
        报告使用中文 Markdown，不提供直接买卖建议。
        必须只输出 JSON，字段为 overallScore, riskLevel, valuationView, summary, keyAssumptions, keyClaims, markdown。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(SynthesisReportAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
