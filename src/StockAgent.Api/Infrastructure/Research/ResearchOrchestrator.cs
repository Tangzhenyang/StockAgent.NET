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
    private static readonly JsonSerializerOptions ArtifactJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Executes the first runnable research pipeline for one queued task.
    /// 为一个已入队任务执行首个可运行的研究流水线。
    /// </summary>
    public async Task RunAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        try
        {
            var task = await db.ResearchTasks.FirstAsync(x => x.Id == researchTaskId, cancellationToken);
            var dataSourceSettings = await userSettingsService.GetDataSourceRuntimeSettingsAsync(task.UserId, cancellationToken);
            await UpdateTaskStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectStructuredData, 10, cancellationToken);

            var snapshot = await RunStepAsync(
                task,
                ResearchStage.CollectStructuredData,
                "请求行情/财务数据源",
                token => marketDataProvider.GetSnapshotAsync(task.Ticker, dataSourceSettings, token),
                result => $"行情/财务数据源完成：{result.CompanyName}，PE {result.PeRatio:N1}，净利率 {result.NetMarginPercent:N1}%",
                cancellationToken,
                (result, step, token) => SaveStepArtifactAsync(
                    step,
                    "market-snapshot",
                    "行情/财务快照",
                    $"{result.CompanyName}，最新价 {result.LastPrice:N2}，PE {result.PeRatio:N1}",
                    result,
                    token));
            task.CompanyName = snapshot.CompanyName;

            await UpdateTaskStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectPublicEvidence, 30, cancellationToken);
            var documents = await RunStepAsync(
                task,
                ResearchStage.CollectPublicEvidence,
                "请求公告/证据数据源",
                token => webResearchProvider.SearchAsync(
                    task.Ticker,
                    snapshot.CompanyName,
                    dataSourceSettings,
                    token),
                result => $"获取到 {result.Count} 条证据文档",
                cancellationToken,
                (result, step, token) => SaveStepArtifactAsync(
                    step,
                    "source-documents",
                    "公告/证据源列表",
                    $"获取到 {result.Count} 条源文档",
                    result.Select(x => new
                    {
                        x.Title,
                        x.Url,
                        x.SourceType,
                        x.PublishedAt,
                        TextLength = x.Text.Length
                    }).ToList(),
                    token));

            logger.LogInformation("Collected {DocumentCount} evidence documents for {Ticker}", documents.Count, task.Ticker);

            await UpdateTaskStatusAsync(task, ResearchTaskStatus.IngestingDocuments, ResearchStage.IngestAndIndexDocuments, 55, cancellationToken);
            var evidenceCards = await RunStepAsync(
                task,
                ResearchStage.IngestAndIndexDocuments,
                $"解析 {documents.Count} 条文档并生成证据卡片",
                token => IngestDocumentsAsync(task, documents, token),
                result => $"生成 {result.Count} 张证据卡片",
                cancellationToken,
                (result, step, token) => SaveIngestionArtifactAsync(task.Id, step, result, token));

            await UpdateTaskStatusAsync(task, ResearchTaskStatus.Analyzing, ResearchStage.AnalyzeWithSemanticKernel, 75, cancellationToken);
            var researchSettings = await userSettingsService.GetResearchSettingsAsync(task.UserId, cancellationToken);
            var modelSettings = await userSettingsService.GetModelRuntimeSettingsAsync(task.UserId, cancellationToken);
            var selectedEvidence = contextBudgetManager.SelectEvidence(evidenceCards, researchSettings.MaxEvidenceCards);
            var analysis = await RunStepAsync(
                task,
                ResearchStage.AnalyzeWithSemanticKernel,
                "运行多 Agent 分析链路",
                token => analysisService.AnalyzeAsync(
                    task.Id,
                    snapshot,
                    selectedEvidence,
                    modelSettings,
                    task.Language,
                    token),
                result => $"多 Agent 分析完成：{string.Join("，", result.AgentTraces ?? [])}",
                cancellationToken,
                (result, step, token) => SaveAnalysisArtifactAsync(task.Id, step, result, token));

            await UpdateTaskStatusAsync(task, ResearchTaskStatus.GeneratingReport, ResearchStage.GenerateReport, 90, cancellationToken);
            await RunStepAsync(
                task,
                ResearchStage.GenerateReport,
                "生成 Markdown/HTML 研究报告",
                token =>
                {
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
                    return Task.FromResult(generatedReport.Score.OverallScore);
                },
                score => $"报告生成完成：综合评分 {score}",
                cancellationToken,
                (score, step, token) => SaveStepArtifactAsync(
                    step,
                    "generated-report",
                    "报告生成结果",
                    $"综合评分 {score}",
                    new { OverallScore = score, task.Language },
                    token));
            await db.SaveChangesAsync(cancellationToken);

            await UpdateTaskStatusAsync(task, ResearchTaskStatus.Ready, ResearchStage.GenerateReport, 100, cancellationToken);
        }
        catch (Exception exception)
        {
            await MarkTaskFailedAsync(researchTaskId, exception, cancellationToken);
            throw;
        }
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

    private async Task<T> RunStepAsync<T>(
        ResearchTask task,
        ResearchStage stage,
        string inputSummary,
        Func<CancellationToken, Task<T>> action,
        Func<T, string> outputSummaryFactory,
        CancellationToken cancellationToken,
        Func<T, ResearchStep, CancellationToken, Task>? artifactWriter = null)
    {
        var step = new ResearchStep
        {
            ResearchTaskId = task.Id,
            StepName = stage,
            Status = StepStatus.Running,
            StartedAt = DateTimeOffset.UtcNow,
            InputSummary = Truncate(inputSummary, 2000)
        };
        db.ResearchSteps.Add(step);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var result = await action(cancellationToken);
            step.Status = StepStatus.Succeeded;
            step.CompletedAt = DateTimeOffset.UtcNow;
            step.OutputSummary = Truncate(outputSummaryFactory(result), 2000);
            if (artifactWriter is not null)
            {
                await artifactWriter(result, step, cancellationToken);
            }

            await db.SaveChangesAsync(cancellationToken);
            return result;
        }
        catch (Exception exception)
        {
            step.Status = StepStatus.Failed;
            step.CompletedAt = DateTimeOffset.UtcNow;
            step.ErrorMessage = Truncate(exception.Message, 4000);
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task UpdateTaskStatusAsync(
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
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task MarkTaskFailedAsync(Guid researchTaskId, Exception exception, CancellationToken cancellationToken)
    {
        var task = await db.ResearchTasks.FirstOrDefaultAsync(x => x.Id == researchTaskId, CancellationToken.None);
        if (task is null)
        {
            return;
        }

        task.Status = ResearchTaskStatus.Failed;
        task.ErrorMessage = Truncate(exception.Message, 4000);
        task.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(cancellationToken.IsCancellationRequested ? CancellationToken.None : cancellationToken);
    }

    private Task SaveStepArtifactAsync(
        ResearchStep step,
        string artifactType,
        string title,
        string summary,
        object payload,
        CancellationToken cancellationToken)
    {
        db.ResearchStepArtifacts.Add(new ResearchStepArtifact
        {
            ResearchTaskId = step.ResearchTaskId,
            ResearchStepId = step.Id,
            Stage = step.StepName,
            ArtifactType = artifactType,
            Title = title,
            Summary = Truncate(summary, 1000),
            JsonPayload = JsonSerializer.Serialize(payload, ArtifactJsonOptions)
        });
        return Task.CompletedTask;
    }

    private async Task SaveIngestionArtifactAsync(
        Guid researchTaskId,
        ResearchStep step,
        IReadOnlyList<EvidenceCard> evidenceCards,
        CancellationToken cancellationToken)
    {
        var sourceIds = evidenceCards.Select(x => x.DocumentSourceId).Distinct().ToList();
        var sources = await db.DocumentSources
            .Where(x => x.ResearchTaskId == researchTaskId && sourceIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var chunks = await db.DocumentChunks
            .Where(x => sourceIds.Contains(x.DocumentSourceId))
            .ToListAsync(cancellationToken);
        var payload = sources.Select(source => new
        {
            source.Id,
            source.Title,
            source.Url,
            source.SourceType,
            source.PublishedAt,
            ChunkCount = chunks.Count(x => x.DocumentSourceId == source.Id),
            EvidenceCards = evidenceCards
                .Where(x => x.DocumentSourceId == source.Id)
                .Select(x => new
                {
                    x.Id,
                    x.Claim,
                    x.Snippet,
                    x.Confidence,
                    x.Relevance,
                    x.ReportSection,
                    x.SourceDate
                })
                .ToList()
        }).ToList();

        await SaveStepArtifactAsync(
            step,
            "ingested-evidence",
            "文档入库与证据卡",
            $"入库 {sources.Count} 个源文档，生成 {evidenceCards.Count} 张证据卡片",
            payload,
            cancellationToken);
    }

    private async Task SaveAnalysisArtifactAsync(
        Guid researchTaskId,
        ResearchStep step,
        AiAnalysisResult analysis,
        CancellationToken cancellationToken)
    {
        var invocations = await db.ModelInvocations
            .Where(x => x.ResearchTaskId == researchTaskId)
            .OrderBy(x => x.CreatedAt)
            .Select(x => new
            {
                x.StepName,
                x.Provider,
                x.ModelName,
                x.DurationMs,
                x.Status,
                x.ErrorMessage,
                x.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var payload = new
        {
            analysis.OverallScore,
            analysis.RiskLevel,
            analysis.ValuationView,
            analysis.Summary,
            analysis.KeyAssumptions,
            analysis.AgentTraces,
            ModelInvocations = invocations
        };

        await SaveStepArtifactAsync(
            step,
            "agent-analysis",
            "多 Agent 分析详情",
            $"评分 {analysis.OverallScore}，风险 {analysis.RiskLevel}",
            payload,
            cancellationToken);
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
