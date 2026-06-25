using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Analyzes industry profile, sector trend, and recent industry news. 分析行业画像、赛道趋势和近期行业消息。</summary>
public sealed class IndustryResearchAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<IndustryResearchAgentInput, IndustryResearchAgentOutput>(
        chatClient,
        modelSettings ?? TestSettings)
{
    private static readonly ModelRuntimeSettings TestSettings = new(
        "OpenAICompatible",
        "https://example.test/v1",
        "test-model",
        "test-key");

    /// <inheritdoc />
    public override string Name => "IndustryResearchAgent";

    /// <inheritdoc />
    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究中的行业与赛道分析 Agent。
        你只能基于输入的行业画像、关键词、近期行业新闻和股票基本信息进行分析。
        重点分析行业景气度、产业链位置、近期新闻影响、机会、风险和后续需要跟踪的问题。
        对无法从输入确认的行业判断必须标为待验证，不要写成确定事实。
        必须只输出 JSON，字段为 industryView, opportunities, risks, newsHighlights, followUpQuestions。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(IndustryResearchAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
