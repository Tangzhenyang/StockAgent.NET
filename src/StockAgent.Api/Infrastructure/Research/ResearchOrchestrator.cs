using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Documents;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Reports;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.Research;

/// <summary>
/// Durable manager for the stock research pipeline. It owns stage transitions and delegates specialized work to providers.
/// 股票研究流水线的持久化管理器，负责阶段切换并将专门工作委托给提供器。
/// </summary>
public sealed class ResearchOrchestrator(
    StockAgentDbContext db,
    IMarketDataProvider marketDataProvider,
    IWebResearchProvider webResearchProvider,
    DocumentChunker documentChunker,
    ContextBudgetManager contextBudgetManager,
    IResearchAnalysisService analysisService,
    ReportGenerator reportGenerator,
    UserSettingsService userSettingsService,
    ILogger<ResearchOrchestrator> logger)
{
    /// <summary>
    /// Executes the first runnable research pipeline for one queued task.
    /// 为一个已入队任务执行首个可运行的研究流水线。
    /// </summary>
    public async Task RunAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        var task = await db.ResearchTasks.FirstAsync(x => x.Id == researchTaskId, cancellationToken);
        var dataSourceSettings = await userSettingsService.GetDataSourceRuntimeSettingsAsync(task.UserId, cancellationToken);
        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectStructuredData, 10, cancellationToken);

        var snapshot = await marketDataProvider.GetSnapshotAsync(task.Ticker, dataSourceSettings, cancellationToken);
        task.CompanyName = snapshot.CompanyName;

        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectPublicEvidence, 30, cancellationToken);
        var documents = await webResearchProvider.SearchAsync(
            task.Ticker,
            snapshot.CompanyName,
            dataSourceSettings,
            cancellationToken);

        logger.LogInformation("Collected {DocumentCount} evidence documents for {Ticker}", documents.Count, task.Ticker);

        await SetStatusAsync(task, ResearchTaskStatus.IngestingDocuments, ResearchStage.IngestAndIndexDocuments, 55, cancellationToken);
        var evidenceCards = await IngestDocumentsAsync(task, documents, cancellationToken);

        await SetStatusAsync(task, ResearchTaskStatus.Analyzing, ResearchStage.AnalyzeWithSemanticKernel, 75, cancellationToken);
        var researchSettings = await userSettingsService.GetResearchSettingsAsync(task.UserId, cancellationToken);
        var modelSettings = await userSettingsService.GetModelRuntimeSettingsAsync(task.UserId, cancellationToken);
        var selectedEvidence = contextBudgetManager.SelectEvidence(evidenceCards, researchSettings.MaxEvidenceCards);
        var analysis = await analysisService.AnalyzeAsync(
            task.Id,
            snapshot,
            selectedEvidence,
            modelSettings,
            task.Language,
            cancellationToken);

        await SetStatusAsync(task, ResearchTaskStatus.GeneratingReport, ResearchStage.GenerateReport, 90, cancellationToken);
        var generatedReport = reportGenerator.Generate(snapshot, analysis, selectedEvidence);
        db.ResearchReports.Add(new ResearchReport
        {
            ResearchTaskId = task.Id,
            Language = task.Language,
            Markdown = generatedReport.Markdown,
            Html = generatedReport.Html,
            RatingJson = JsonSerializer.Serialize(generatedReport.Score),
            DataCutoffAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        await SetStatusAsync(task, ResearchTaskStatus.Ready, ResearchStage.GenerateReport, 100, cancellationToken);
    }

    private async Task<IReadOnlyList<EvidenceCard>> IngestDocumentsAsync(
        ResearchTask task,
        IReadOnlyList<WebEvidenceDocument> documents,
        CancellationToken cancellationToken)
    {
        var evidenceCards = new List<EvidenceCard>();

        foreach (var document in documents)
        {
            var source = new DocumentSource
            {
                ResearchTaskId = task.Id,
                Url = document.Url,
                Title = document.Title,
                SourceType = document.SourceType,
                PublishedAt = document.PublishedAt,
                ContentHash = CreateContentHash(document.Text)
            };
            db.DocumentSources.Add(source);

            foreach (var chunk in documentChunker.Chunk(document.Text, maxCharacters: 600))
            {
                var documentChunk = new DocumentChunk
                {
                    DocumentSourceId = source.Id,
                    ChunkIndex = chunk.Index,
                    Text = chunk.Text,
                    TokenEstimate = chunk.TokenEstimate
                };
                db.DocumentChunks.Add(documentChunk);

                evidenceCards.Add(new EvidenceCard
                {
                    ResearchTaskId = task.Id,
                    DocumentSourceId = source.Id,
                    DocumentChunkId = documentChunk.Id,
                    Claim = document.Title,
                    Snippet = CreateSnippet(chunk.Text),
                    Confidence = 0.82m,
                    Relevance = document.SourceType == "annual-report" ? 0.92m : 0.78m,
                    SourceDate = document.PublishedAt,
                    ReportSection = document.SourceType == "annual-report" ? "Financials" : "Business"
                });
            }
        }

        db.EvidenceCards.AddRange(evidenceCards);
        await db.SaveChangesAsync(cancellationToken);
        return evidenceCards;
    }

    private static string CreateContentHash(string text)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
    }

    private static string CreateSnippet(string text)
    {
        var normalized = text.Trim();
        return normalized.Length <= 160 ? normalized : normalized[..160];
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
