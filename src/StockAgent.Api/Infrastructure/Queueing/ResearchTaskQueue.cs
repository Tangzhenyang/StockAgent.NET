using System.Threading.Channels;

namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// Channel-backed in-memory queue used by the first-version modular monolith.
/// 首个版本模块化单体使用的基于 Channel 的内存队列。
/// </summary>
public sealed class ResearchTaskQueue : IResearchTaskQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    /// <inheritdoc />
    public async Task QueueAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(researchTaskId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
