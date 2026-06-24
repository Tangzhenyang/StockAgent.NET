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

        return app;
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
