namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// Exports generated report HTML to a PDF file.
/// </summary>
public interface IPdfExportService
{
    /// <summary>Writes a PDF file and returns the absolute path.</summary>
    Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken);
}
