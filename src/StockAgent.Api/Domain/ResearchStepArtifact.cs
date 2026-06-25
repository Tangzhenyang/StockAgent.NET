namespace StockAgent.Api.Domain;

/// <summary>
/// Structured artifact captured for a research pipeline step.
/// 研究流水线步骤捕获的结构化产物。
/// </summary>
public sealed class ResearchStepArtifact
{
    /// <summary>Unique artifact identifier. 唯一产物标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Parent research step identifier. 父级研究步骤标识符。</summary>
    public Guid ResearchStepId { get; set; }
    /// <summary>Pipeline stage that produced this artifact. 生成此产物的流水线阶段。</summary>
    public ResearchStage Stage { get; set; }
    /// <summary>Machine-readable artifact category. 机器可读的产物类别。</summary>
    public string ArtifactType { get; set; } = string.Empty;
    /// <summary>Human-readable artifact title. 人类可读的产物标题。</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Short display summary. 展示用简短摘要。</summary>
    public string? Summary { get; set; }
    /// <summary>Sanitized JSON payload safe for UI diagnostics. 可安全用于 UI 诊断的脱敏 JSON 载荷。</summary>
    public string JsonPayload { get; set; } = "{}";
    /// <summary>UTC creation timestamp. UTC 创建时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
