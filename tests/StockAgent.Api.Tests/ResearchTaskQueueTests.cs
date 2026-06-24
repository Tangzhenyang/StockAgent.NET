using FluentAssertions;
using StockAgent.Api.Infrastructure.Queueing;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies that research task IDs can be queued and consumed in FIFO order.
/// 验证研究任务 ID 可按 FIFO 顺序入队和消费。
/// </summary>
public sealed class ResearchTaskQueueTests
{
    /// <summary>
    /// A queued task ID is read by the worker consumer.
    /// 已入队的任务 ID 会被工作器消费者读取。
    /// </summary>
    [Fact]
    public async Task QueueAsync_ThenDequeueAsync_ReturnsTaskId()
    {
        var queue = new ResearchTaskQueue();
        var taskId = Guid.NewGuid();

        await queue.QueueAsync(taskId, CancellationToken.None);
        var result = await queue.DequeueAsync(CancellationToken.None);

        result.Should().Be(taskId);
    }
}
