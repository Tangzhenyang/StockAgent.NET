namespace StockAgent.Api.Domain;

/// <summary>Audit record for one model or deterministic analysis invocation. 一次模型或确定性分析调用的审计记录。</summary>
public sealed class ModelInvocation
{
    /// <summary>Unique invocation identifier. 唯一调用标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Pipeline stage that triggered the invocation. 触发调用的流水线阶段。</summary>
    public string StepName { get; set; } = string.Empty;
    /// <summary>Provider name such as OpenAI, Compatible, or Deterministic. 提供器名称，例如 OpenAI、Compatible 或 Deterministic。</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Model name or deterministic analyzer name. 模型名称或确定性分析器名称。</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Prompt token count when available. 可用时的提示词 token 数。</summary>
    public int? PromptTokens { get; set; }
    /// <summary>Completion token count when available. 可用时的补全 token 数。</summary>
    public int? CompletionTokens { get; set; }
    /// <summary>Invocation duration in milliseconds. 调用耗时，单位毫秒。</summary>
    public long DurationMs { get; set; }
    /// <summary>Invocation status such as Succeeded or Failed. 调用状态，例如 Succeeded 或 Failed。</summary>
    public string Status { get; set; } = "Succeeded";
    /// <summary>Failure message safe to persist. 可安全持久化的失败信息。</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>UTC creation timestamp. UTC 创建时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
