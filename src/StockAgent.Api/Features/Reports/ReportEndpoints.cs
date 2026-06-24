using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Security;

namespace StockAgent.Api.Features.Reports;

/// <summary>
/// Endpoints for reading generated research reports.
/// 用于读取已生成研究报告的端点。
/// </summary>
public static class ReportEndpoints
{
    /// <summary>Maps report endpoints. 映射报告端点。</summary>
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/report", async (
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

            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            return report is null ? Results.NotFound() : Results.Ok(report);
        }).WithTags("Reports").RequireAuthorization();

        return app;
    }
}
