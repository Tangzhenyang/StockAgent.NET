using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Analyzes valuation, market data, and financial quality. 分析估值、行情和财务质量。</summary>
public sealed class MarketFinancialAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<MarketFinancialAgentInput, MarketFinancialAgentOutput>(
        chatClient,
        modelSettings ?? TestSettings)
{
    private static readonly ModelRuntimeSettings TestSettings = new(
        "OpenAICompatible",
        "https://example.test/v1",
        "test-model",
        "test-key");

    /// <inheritdoc />
    public override string Name => "MarketFinancialAgent";

    /// <inheritdoc />
    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究中的行情与财务分析 Agent。
        你只分析输入中的结构化行情、估值、市值、收入增长和净利率。
        不得引用未提供的数据，不得给出直接买卖建议。
        必须只输出 JSON，字段为 score, valuationView, strengths, risks, followUpQuestions。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(MarketFinancialAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
