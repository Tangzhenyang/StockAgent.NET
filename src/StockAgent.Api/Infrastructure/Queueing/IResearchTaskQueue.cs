namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// In-process queue for research task identifiers awaiting background execution.
/// 等待后台执行的研究任务标识符进程内队列。
/// </summary>
public interface IResearchTaskQueue
{
    /// <summary>Queues a task identifier for worker execution. 将任务标识符入队供工作器执行。</summary>
    Task QueueAsync(Guid researchTaskId, CancellationToken cancellationToken);

    /// <summary>Reads the next queued task identifier. 读取下一个已入队的任务标识符。</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
