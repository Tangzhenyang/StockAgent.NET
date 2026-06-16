using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Infrastructure.Research;

/// <summary>
/// Durable manager for the stock research pipeline. It owns stage transitions and delegates specialized work to providers.
/// </summary>
public sealed class ResearchOrchestrator(
    StockAgentDbContext db,
    IMarketDataProvider marketDataProvider,
    IWebResearchProvider webResearchProvider,
    ILogger<ResearchOrchestrator> logger)
{
    /// <summary>
    /// Executes the first runnable research pipeline for one queued task.
    /// </summary>
    public async Task RunAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        var task = await db.ResearchTasks.FirstAsync(x => x.Id == researchTaskId, cancellationToken);
        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectStructuredData, 10, cancellationToken);

        var snapshot = await marketDataProvider.GetSnapshotAsync(task.Ticker, cancellationToken);
        task.CompanyName = snapshot.CompanyName;

        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectPublicEvidence, 30, cancellationToken);
        var documents = await webResearchProvider.SearchAsync(task.Ticker, snapshot.CompanyName, cancellationToken);

        logger.LogInformation("Collected {DocumentCount} fake evidence documents for {Ticker}", documents.Count, task.Ticker);

        await SetStatusAsync(task, ResearchTaskStatus.Ready, ResearchStage.GenerateReport, 100, cancellationToken);
    }

    private async Task SetStatusAsync(
        ResearchTask task,
        ResearchTaskStatus status,
        ResearchStage stage,
        int progress,
        CancellationToken cancellationToken)
    {
        task.Status = status;
        task.CurrentStage = stage;
        task.ProgressPercent = progress;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        db.ResearchSteps.Add(new ResearchStep
        {
            ResearchTaskId = task.Id,
            StepName = stage,
            Status = StepStatus.Succeeded,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            OutputSummary = $"Stage {stage} completed."
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
