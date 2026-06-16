using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Infrastructure.Research;

namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// Background worker that consumes queued research task IDs and runs the durable orchestrator.
/// </summary>
public sealed class ResearchWorker(
    IResearchTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ResearchWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var taskId = await queue.DequeueAsync(stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ResearchOrchestrator>();

            try
            {
                await orchestrator.RunAsync(taskId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Research task {TaskId} failed in background worker.", taskId);
            }
        }
    }
}
