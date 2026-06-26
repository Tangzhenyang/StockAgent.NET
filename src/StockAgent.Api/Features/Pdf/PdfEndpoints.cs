using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Security;

namespace StockAgent.Api.Features.Pdf;

/// <summary>
/// Endpoints for requesting and downloading PDF exports.
/// 用于请求和下载 PDF 导出的端点。
/// </summary>
public static class PdfEndpoints
{
    /// <summary>Maps PDF endpoints. 映射 PDF 端点。</summary>
    public static IEndpointRouteBuilder MapPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/research-tasks/{id:guid}/pdf", async (
            Guid id,
            StockAgentDbContext db,
            IPdfExportService pdfExportService,
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

            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            if (report is null)
            {
                return Results.NotFound();
            }

            var requestedAt = DateTimeOffset.UtcNow;
            string path;
            try
            {
                path = await pdfExportService.ExportAsync(id, report.Html, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                return Results.Problem(
                    title: "PDF export failed",
                    detail: $"PDF export failed. Please verify Chromium is available in the API container. {exception.Message}",
                    statusCode: StatusCodes.Status500InternalServerError);
            }

            db.PdfExports.Add(new PdfExport
            {
                ResearchTaskId = id,
                Status = "Completed",
                FilePath = path,
                RequestedAt = requestedAt,
                CompletedAt = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync(cancellationToken);

            var fileName = CreatePdfFileName(task.Ticker);
            return Results.Ok(new PdfExportResponse(
                id,
                $"/api/research-tasks/{id}/pdf/download",
                fileName,
                "Completed"));
        }).WithTags("PDF").RequireAuthorization();

        app.MapGet("/api/research-tasks/{id:guid}/pdf/download", async (
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

            var export = await db.PdfExports
                .Where(x => x.ResearchTaskId == id && x.Status == "Completed" && x.FilePath != null)
                .OrderByDescending(x => x.CompletedAt ?? x.RequestedAt)
                .FirstOrDefaultAsync(cancellationToken);
            if (export?.FilePath is null || !File.Exists(export.FilePath))
            {
                return Results.NotFound();
            }

            return Results.File(
                export.FilePath,
                "application/pdf",
                CreatePdfFileName(task.Ticker));
        }).WithTags("PDF").RequireAuthorization();

        return app;
    }

    private static string CreatePdfFileName(string ticker)
    {
        var safeTicker = string.Join("_", ticker.Split(Path.GetInvalidFileNameChars()));
        return $"{safeTicker}-research-report.pdf";
    }
}

/// <summary>
/// Response returned after a PDF export has been generated.
/// PDF 导出生成后返回的响应。
/// </summary>
/// <param name="ResearchTaskId">Research task identifier. 研究任务标识符。</param>
/// <param name="DownloadUrl">Relative URL that streams the generated PDF. 流式返回已生成 PDF 的相对 URL。</param>
/// <param name="FileName">Suggested browser download file name. 建议浏览器下载文件名。</param>
/// <param name="Status">Export status. 导出状态。</param>
public sealed record PdfExportResponse(Guid ResearchTaskId, string DownloadUrl, string FileName, string Status);
