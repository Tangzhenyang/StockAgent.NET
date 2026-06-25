using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;
using StockAgent.Api.Infrastructure.Security;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Minimal API endpoints for creating and reading research tasks.
/// 用于创建和读取研究任务的 Minimal API 端点。
/// </summary>
public static class ResearchTaskEndpoints
{
    private static readonly TimeSpan LongRunningStepThreshold = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan StaleActiveTaskThreshold = TimeSpan.FromMinutes(10);

    /// <summary>Maps research task endpoints. 映射研究任务端点。</summary>
    public static IEndpointRouteBuilder MapResearchTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/research-tasks").WithTags("Research Tasks").RequireAuthorization();

        group.MapPost("/", async (
            CreateResearchTaskRequest request,
            StockAgentDbContext db,
            IResearchTaskQueue queue,
            ICurrentUser currentUser,
            UserSettingsService settingsService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.Ticker))
            {
                return Results.BadRequest(new { error = "Ticker is required." });
            }

            NormalizedTicker normalized;
            try
            {
                normalized = TickerNormalizer.Normalize(request.Ticker, request.Market);
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { error = exception.Message });
            }

            var userId = currentUser.RequireUserId();
            var researchSettings = await settingsService.GetResearchSettingsAsync(userId, cancellationToken);
            var task = new ResearchTask
            {
                UserId = userId,
                Ticker = normalized.Ticker,
                Market = normalized.Market,
                Language = string.IsNullOrWhiteSpace(request.Language) ? researchSettings.DefaultLanguage : request.Language.Trim()
            };

            db.ResearchTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            await queue.QueueAsync(task.Id, cancellationToken);

            return Results.Created($"/api/research-tasks/{task.Id}", ToResponse(task));
        });

        group.MapGet("/", async (
            string? status,
            StockAgentDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.RequireUserId();
            var query = db.ResearchTasks.Where(x => x.UserId == userId);
            if (string.Equals(status, "completed", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(x => x.Status == ResearchTaskStatus.Ready || x.Status == ResearchTaskStatus.Completed);
            }

            var tasks = await query
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ResearchTaskResponse(
                    x.Id,
                    x.Ticker,
                    x.Market,
                    x.Status,
                    x.ProgressPercent,
                    x.Language,
                    x.CreatedAt,
                    x.UpdatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(tasks);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            StockAgentDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.RequireUserId();
            var task = await db.ResearchTasks.FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken);
            return task is null ? Results.NotFound() : Results.Ok(ToResponse(task));
        });

        group.MapGet("/{id:guid}/steps", async (
            Guid id,
            StockAgentDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.RequireUserId();
            var ownsTask = await db.ResearchTasks.AnyAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken);
            if (!ownsTask)
            {
                return Results.NotFound();
            }

            var now = DateTimeOffset.UtcNow;
            var stepEntities = await db.ResearchSteps
                .Where(x => x.ResearchTaskId == id)
                .OrderBy(x => x.StartedAt ?? DateTimeOffset.MaxValue)
                .ThenBy(x => x.CompletedAt ?? DateTimeOffset.MaxValue)
                .ToListAsync(cancellationToken);
            var steps = stepEntities
                .Select(x => new ResearchStepResponse(
                    x.Id,
                    x.StepName,
                    x.Status,
                    x.RetryCount,
                    x.StartedAt,
                    x.CompletedAt,
                    x.StartedAt == null
                        ? null
                        : Convert.ToInt64(((x.CompletedAt ?? now) - x.StartedAt.Value).TotalMilliseconds),
                    x.InputSummary,
                    x.OutputSummary,
                    x.ErrorMessage,
                    x.Status == StepStatus.Running
                    && x.StartedAt != null
                    && now - x.StartedAt.Value >= LongRunningStepThreshold))
                .ToList();

            return Results.Ok(steps);
        });

        group.MapGet("/{id:guid}/steps/{stepId:guid}/artifacts", async (
            Guid id,
            Guid stepId,
            StockAgentDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.RequireUserId();
            var ownsStep = await db.ResearchSteps.AnyAsync(
                x => x.Id == stepId && x.ResearchTaskId == id && x.ResearchTask!.UserId == userId,
                cancellationToken);
            if (!ownsStep)
            {
                return Results.NotFound();
            }

            var artifacts = await db.ResearchStepArtifacts
                .Where(x => x.ResearchTaskId == id && x.ResearchStepId == stepId)
                .OrderBy(x => x.CreatedAt)
                .Select(x => new ResearchStepArtifactResponse(
                    x.Id,
                    x.Stage,
                    x.ArtifactType,
                    x.Title,
                    x.Summary,
                    x.JsonPayload,
                    x.CreatedAt))
                .ToListAsync(cancellationToken);

            return Results.Ok(artifacts);
        });

        group.MapDelete("/{id:guid}", async (
            Guid id,
            StockAgentDbContext db,
            ICurrentUser currentUser,
            CancellationToken cancellationToken) =>
        {
            var userId = currentUser.RequireUserId();
            var task = await db.ResearchTasks.FirstOrDefaultAsync(
                x => x.Id == id && x.UserId == userId,
                cancellationToken);
            if (task is null)
            {
                return Results.NotFound();
            }

            if (!CanDelete(task, DateTimeOffset.UtcNow))
            {
                return Results.Conflict(new { error = "Only terminal tasks or active tasks stale for more than 10 minutes can be deleted." });
            }

            var sources = await db.DocumentSources
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var sourceIds = sources.Select(x => x.Id).ToList();
            var chunks = await db.DocumentChunks
                .Where(x => sourceIds.Contains(x.DocumentSourceId))
                .ToListAsync(cancellationToken);
            var evidenceCards = await db.EvidenceCards
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var reports = await db.ResearchReports
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var pdfExports = await db.PdfExports
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var invocations = await db.ModelInvocations
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var steps = await db.ResearchSteps
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);
            var artifacts = await db.ResearchStepArtifacts
                .Where(x => x.ResearchTaskId == id)
                .ToListAsync(cancellationToken);

            db.DocumentChunks.RemoveRange(chunks);
            db.EvidenceCards.RemoveRange(evidenceCards);
            db.DocumentSources.RemoveRange(sources);
            db.ResearchReports.RemoveRange(reports);
            db.PdfExports.RemoveRange(pdfExports);
            db.ModelInvocations.RemoveRange(invocations);
            db.ResearchStepArtifacts.RemoveRange(artifacts);
            db.ResearchSteps.RemoveRange(steps);
            db.ResearchTasks.Remove(task);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        });

        return app;
    }

    private static bool CanDelete(ResearchTask task, DateTimeOffset now)
    {
        if (task.Status is ResearchTaskStatus.Failed
            or ResearchTaskStatus.Ready
            or ResearchTaskStatus.Completed
            or ResearchTaskStatus.Cancelled)
        {
            return true;
        }

        return now - task.UpdatedAt >= StaleActiveTaskThreshold;
    }

    private static ResearchTaskResponse ToResponse(ResearchTask task)
    {
        return new ResearchTaskResponse(
            task.Id,
            task.Ticker,
            task.Market,
            task.Status,
            task.ProgressPercent,
            task.Language,
            task.CreatedAt,
            task.UpdatedAt);
    }
}
