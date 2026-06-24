namespace StockAgent.Api.Domain;

/// <summary>
/// Durable audit record for one stage of a research task.
/// 研究任务某一阶段的持久化审计记录。
/// </summary>
public sealed class ResearchStep
{
    /// <summary>Unique step identifier. 唯一步骤标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Parent research task. 父级研究任务。</summary>
    public ResearchTask? ResearchTask { get; set; }
    /// <summary>Pipeline stage represented by this step. 此步骤表示的流水线阶段。</summary>
    public ResearchStage StepName { get; set; }
    /// <summary>Execution status for this step. 此步骤的执行状态。</summary>
    public StepStatus Status { get; set; } = StepStatus.Pending;
    /// <summary>Number of retry attempts for this step. 此步骤的重试次数。</summary>
    public int RetryCount { get; set; }
    /// <summary>UTC timestamp when execution started. 执行开始时的 UTC 时间戳。</summary>
    public DateTimeOffset? StartedAt { get; set; }
    /// <summary>UTC timestamp when execution ended. 执行结束时的 UTC 时间戳。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Short summary of the step input. 步骤输入的简短摘要。</summary>
    public string? InputSummary { get; set; }
    /// <summary>Short summary of the step output. 步骤输出的简短摘要。</summary>
    public string? OutputSummary { get; set; }
    /// <summary>Failure details safe to display in the UI. 可在 UI 中显示的失败详情。</summary>
    public string? ErrorMessage { get; set; }
}
