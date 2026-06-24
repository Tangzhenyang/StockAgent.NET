using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Analyzes filings and public evidence cards. 分析公告和公开证据卡。</summary>
public sealed class EvidenceFilingAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<EvidenceFilingAgentInput, EvidenceFilingAgentOutput>(
        chatClient,
        modelSettings ?? TestSettings)
{
    private static readonly ModelRuntimeSettings TestSettings = new(
        "OpenAICompatible",
        "https://example.test/v1",
        "test-model",
        "test-key");

    /// <inheritdoc />
    public override string Name => "EvidenceFilingAgent";

    /// <inheritdoc />
    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究中的公告与公开证据分析 Agent。
        你只能基于输入 evidencePack 提取事实、正面证据、负面证据和不确定性。
        每条关键事实必须能追溯到 evidenceCardId。
        必须只输出 JSON，字段为 positiveFacts, negativeFacts, uncertainties, citations。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(EvidenceFilingAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
