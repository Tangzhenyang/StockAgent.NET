using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Reports;

/// <summary>
/// Endpoints for reading generated research reports.
/// </summary>
public static class ReportEndpoints
{
    /// <summary>Maps report endpoints.</summary>
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/report", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            return report is null ? Results.NotFound() : Results.Ok(report);
        }).WithTags("Reports");

        return app;
    }
}
