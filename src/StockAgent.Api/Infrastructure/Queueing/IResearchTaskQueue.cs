namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// In-process queue for research task identifiers awaiting background execution.
/// </summary>
public interface IResearchTaskQueue
{
    /// <summary>Queues a task identifier for worker execution.</summary>
    Task QueueAsync(Guid researchTaskId, CancellationToken cancellationToken);

    /// <summary>Reads the next queued task identifier.</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
