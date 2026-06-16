using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Evidence;

/// <summary>
/// Endpoints for reading evidence cards and source metadata.
/// </summary>
public static class EvidenceEndpoints
{
    /// <summary>Maps evidence endpoints.</summary>
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/evidence", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var cards = await db.EvidenceCards.Where(x => x.ResearchTaskId == id).ToListAsync(cancellationToken);
            return Results.Ok(cards);
        }).WithTags("Evidence");

        return app;
    }
}
