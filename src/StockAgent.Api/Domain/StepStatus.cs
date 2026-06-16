namespace StockAgent.Api.Domain;

/// <summary>
/// Execution state for an individual research pipeline step.
/// </summary>
public enum StepStatus
{
    /// <summary>The step has not started.</summary>
    Pending = 1,
    /// <summary>The step is currently running.</summary>
    Running = 2,
    /// <summary>The step completed successfully.</summary>
    Succeeded = 3,
    /// <summary>The step failed and can be inspected or retried.</summary>
    Failed = 4,
    /// <summary>The step was skipped because the task was cancelled or no longer needed.</summary>
    Skipped = 5
}
