using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Pdf;

/// <summary>
/// Endpoints for requesting and downloading PDF exports.
/// </summary>
public static class PdfEndpoints
{
    /// <summary>Maps PDF endpoints.</summary>
    public static IEndpointRouteBuilder MapPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/research-tasks/{id:guid}/pdf", async (
            Guid id,
            StockAgentDbContext db,
            IPdfExportService pdfExportService,
            CancellationToken cancellationToken) =>
        {
            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            if (report is null)
            {
                return Results.NotFound();
            }

            var path = await pdfExportService.ExportAsync(id, report.Html, cancellationToken);
            return Results.Ok(new { researchTaskId = id, filePath = path, status = "Completed" });
        }).WithTags("PDF");

        return app;
    }
}
