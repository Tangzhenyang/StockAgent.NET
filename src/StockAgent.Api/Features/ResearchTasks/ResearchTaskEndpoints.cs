using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Minimal API endpoints for creating and reading research tasks.
/// </summary>
public static class ResearchTaskEndpoints
{
    /// <summary>Maps research task endpoints.</summary>
    public static IEndpointRouteBuilder MapResearchTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/research-tasks").WithTags("Research Tasks");

        group.MapPost("/", async (
            CreateResearchTaskRequest request,
            StockAgentDbContext db,
            IResearchTaskQueue queue,
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

            var task = new ResearchTask
            {
                Ticker = normalized.Ticker,
                Market = normalized.Market,
                Language = string.IsNullOrWhiteSpace(request.Language) ? "zh-CN" : request.Language.Trim()
            };

            db.ResearchTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            await queue.QueueAsync(task.Id, cancellationToken);

            return Results.Created($"/api/research-tasks/{task.Id}", ToResponse(task));
        });

        group.MapGet("/", async (StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var tasks = await db.ResearchTasks
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ResearchTaskResponse(x.Id, x.Ticker, x.Market, x.Status, x.ProgressPercent, x.Language))
                .ToListAsync(cancellationToken);

            return Results.Ok(tasks);
        });

        group.MapGet("/{id:guid}", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var task = await db.ResearchTasks.FindAsync([id], cancellationToken);
            return task is null ? Results.NotFound() : Results.Ok(ToResponse(task));
        });

        return app;
    }

    private static ResearchTaskResponse ToResponse(ResearchTask task)
    {
        return new ResearchTaskResponse(task.Id, task.Ticker, task.Market, task.Status, task.ProgressPercent, task.Language);
    }
}
