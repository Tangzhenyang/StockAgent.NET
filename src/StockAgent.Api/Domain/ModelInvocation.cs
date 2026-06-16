namespace StockAgent.Api.Domain;

/// <summary>Audit record for one model or deterministic analysis invocation.</summary>
public sealed class ModelInvocation
{
    /// <summary>Unique invocation identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Pipeline stage that triggered the invocation.</summary>
    public string StepName { get; set; } = string.Empty;
    /// <summary>Provider name such as OpenAI, Compatible, or Deterministic.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Model name or deterministic analyzer name.</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Prompt token count when available.</summary>
    public int? PromptTokens { get; set; }
    /// <summary>Completion token count when available.</summary>
    public int? CompletionTokens { get; set; }
    /// <summary>Invocation duration in milliseconds.</summary>
    public long DurationMs { get; set; }
    /// <summary>Invocation status such as Succeeded or Failed.</summary>
    public string Status { get; set; } = "Succeeded";
    /// <summary>Failure message safe to persist.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
