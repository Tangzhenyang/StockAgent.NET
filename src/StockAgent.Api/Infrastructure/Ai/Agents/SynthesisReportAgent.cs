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
        只能基于 MarketFinancialAgent、EvidenceFilingAgent 和 IndustryResearchAgent 的输出生成结论。
        不得创造新事实。所有关键结论必须输出到 keyClaims，并绑定 evidenceCardIds。
        如果某个结论无法绑定至少一个 evidenceCardId，不要把它放入 keyClaims。
        报告使用接近券商研报风格的中文 Markdown，不提供直接买卖建议。
        报告必须尽量具体、细化、可复核，优先使用输入中的数字、事实和引用。
        在证据充足时，报告正文目标为约 8000 个中文字符；如果证据不足，也要保留完整研报框架并明确写出缺口。
        报告建议包含：投资摘要、公司概览、行业与竞争格局、行情与估值、财务质量、业务经营、公告与事件解读、行业景气度、产业链位置、关键驱动因素、风险因素、证据不足与无法推断事项、评分评级、后续跟踪指标。
        对没有证据支撑的内容必须写为待验证假设，不要写成确定结论。
        必须只输出 JSON，字段为 overallScore, riskLevel, valuationView, summary, keyAssumptions, keyClaims, markdown。
        """;
    }

    /// <inheritdoc />
    protected override string BuildUserPrompt(SynthesisReportAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
