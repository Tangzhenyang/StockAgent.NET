using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Security;

namespace StockAgent.Api.Features.Evidence;

/// <summary>
/// Endpoints for reading evidence cards and source metadata.
/// 用于读取证据卡和源元数据的端点。
/// </summary>
public static class EvidenceEndpoints
{
    /// <summary>Maps evidence endpoints. 映射证据端点。</summary>
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/evidence", async (
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

            var cards = await db.EvidenceCards.Where(x => x.ResearchTaskId == id).ToListAsync(cancellationToken);
            return Results.Ok(cards);
        }).WithTags("Evidence").RequireAuthorization();

        return app;
    }
}
