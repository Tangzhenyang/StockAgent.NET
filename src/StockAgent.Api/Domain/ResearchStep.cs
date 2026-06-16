namespace StockAgent.Api.Domain;

/// <summary>
/// Durable audit record for one stage of a research task.
/// </summary>
public sealed class ResearchStep
{
    /// <summary>Unique step identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Parent research task.</summary>
    public ResearchTask? ResearchTask { get; set; }
    /// <summary>Pipeline stage represented by this step.</summary>
    public ResearchStage StepName { get; set; }
    /// <summary>Execution status for this step.</summary>
    public StepStatus Status { get; set; } = StepStatus.Pending;
    /// <summary>Number of retry attempts for this step.</summary>
    public int RetryCount { get; set; }
    /// <summary>UTC timestamp when execution started.</summary>
    public DateTimeOffset? StartedAt { get; set; }
    /// <summary>UTC timestamp when execution ended.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Short summary of the step input.</summary>
    public string? InputSummary { get; set; }
    /// <summary>Short summary of the step output.</summary>
    public string? OutputSummary { get; set; }
    /// <summary>Failure details safe to display in the UI.</summary>
    public string? ErrorMessage { get; set; }
}
