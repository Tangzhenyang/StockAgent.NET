using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Base class for agents that ask the model for strict JSON and deserialize it into typed output.
/// 请求模型返回严格 JSON 并反序列化为强类型输出的 Agent 基类。
/// </summary>
public abstract class JsonAgentBase<TInput, TOutput>(
    IModelChatClient chatClient,
    ModelRuntimeSettings modelSettings) : IResearchAgent<TInput, TOutput>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public async Task<TOutput> RunAsync(TInput input, CancellationToken cancellationToken)
    {
        var json = await chatClient.CompleteJsonAsync(
            Name,
            BuildSystemPrompt(),
            BuildUserPrompt(input),
            modelSettings,
            cancellationToken);
        return Deserialize(json);
    }

    /// <summary>Builds the system prompt for the role. 构建角色 system prompt。</summary>
    protected abstract string BuildSystemPrompt();

    /// <summary>Builds the user prompt from typed input. 从强类型输入构建 user prompt。</summary>
    protected abstract string BuildUserPrompt(TInput input);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new FlexibleIntJsonConverter());
        options.Converters.Add(new FlexibleStringListJsonConverter());
        options.Converters.Add(new FlexibleStringJsonConverter());
        options.Converters.Add(new EvidenceCitationJsonConverter());
        return options;
    }

    private static TOutput Deserialize(string json)
    {
        var cleaned = json.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }

        if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3].Trim();
        }

        return JsonSerializer.Deserialize<TOutput>(cleaned, JsonOptions)
               ?? throw new InvalidOperationException($"Model returned empty JSON for {typeof(TOutput).Name}.");
    }
}
