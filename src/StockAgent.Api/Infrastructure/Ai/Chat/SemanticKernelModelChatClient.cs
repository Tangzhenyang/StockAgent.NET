using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using StockAgent.Api.Features.UserSettings;

namespace StockAgent.Api.Infrastructure.Ai.Chat;

/// <summary>
/// Semantic Kernel implementation for OpenAI-compatible chat completion calls.
/// 面向 OpenAI 兼容聊天补全调用的 Semantic Kernel 实现。
/// </summary>
public sealed class SemanticKernelModelChatClient(ILogger<SemanticKernelModelChatClient> logger) : IModelChatClient
{
    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string agentName,
        string systemPrompt,
        string userPrompt,
        ModelRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Model settings are incomplete. Configure provider, base URL, model, and API key.");
        }

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: settings.Model,
            apiKey: settings.ApiKey!,
            endpoint: new Uri(settings.BaseUrl));
        var kernel = builder.Build();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(userPrompt);

        logger.LogInformation("Calling model {Model} for {AgentName}.", settings.Model, agentName);
        var response = await chat.GetChatMessageContentAsync(
            history,
            kernel: kernel,
            cancellationToken: cancellationToken);
        return response.Content ?? string.Empty;
    }
}
