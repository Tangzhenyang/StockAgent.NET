namespace StockAgent.Api.Domain;

/// <summary>
/// Execution state for an individual research pipeline step.
/// 单个研究流水线步骤的执行状态。
/// </summary>
public enum StepStatus
{
    /// <summary>The step has not started. 步骤尚未开始。</summary>
    Pending = 1,
    /// <summary>The step is currently running. 步骤正在运行。</summary>
    Running = 2,
    /// <summary>The step completed successfully. 步骤已成功完成。</summary>
    Succeeded = 3,
    /// <summary>The step failed and can be inspected or retried. 步骤失败，可检查或重试。</summary>
    Failed = 4,
    /// <summary>The step was skipped because the task was cancelled or no longer needed. 步骤因任务取消或不再需要而被跳过。</summary>
    Skipped = 5
}
