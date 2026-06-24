using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Reviews whether the report is evidence-bound and appropriately cautious. 审核报告是否证据充分且表述审慎。</summary>
public sealed class ReviewAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<ReviewAgentInput, ReviewAgentOutput>(
        chatClient,
        modelSettings ?? TestSettings)
{
    private static readonly ModelRuntimeSettings TestSettings = new(
        "OpenAICompatible",
        "https://example.test/v1",
        "test-model",
        "test-key");

    /// <inheritdoc />
    public override string Name => "ReviewAgent";

    /// <inheritdoc />
    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究报告质检 Agent。
        检查报告是否存在没有证据支撑的结论、直接买卖建议、过度确定性表达、引用缺失。
        如果问题不影响报告使用，approved 为 true；如果存在严重问题，approved 为 false。
        必须只输出 JSON，字段为 approved, issues, revisionInstruction。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(ReviewAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
