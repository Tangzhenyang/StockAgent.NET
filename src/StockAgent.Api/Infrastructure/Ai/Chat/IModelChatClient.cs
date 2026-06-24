using StockAgent.Api.Features.UserSettings;

namespace StockAgent.Api.Infrastructure.Ai.Chat;

/// <summary>
/// Testable boundary for model chat completion calls that must return JSON.
/// 必须返回 JSON 的模型聊天补全可测试边界。
/// </summary>
public interface IModelChatClient
{
    /// <summary>Completes one agent prompt and returns raw JSON text. 完成一次 Agent 提示词调用并返回原始 JSON 文本。</summary>
    Task<string> CompleteJsonAsync(
        string agentName,
        string systemPrompt,
        string userPrompt,
        ModelRuntimeSettings settings,
        CancellationToken cancellationToken);
}
