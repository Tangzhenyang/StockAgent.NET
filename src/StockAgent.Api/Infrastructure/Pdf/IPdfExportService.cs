namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// Exports generated report HTML to a PDF file.
/// 将生成的报告 HTML 导出为 PDF 文件。
/// </summary>
public interface IPdfExportService
{
    /// <summary>Writes a PDF file and returns the absolute path. 写入 PDF 文件并返回绝对路径。</summary>
    Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken);
}
