namespace StockAgent.Api.Domain;

/// <summary>PDF export audit record for a research report. 研究报告的 PDF 导出审计记录。</summary>
public sealed class PdfExport
{
    /// <summary>Unique PDF export identifier. 唯一 PDF 导出标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Export status such as Queued, Running, Completed, or Failed. 导出状态，例如 Queued、Running、Completed 或 Failed。</summary>
    public string Status { get; set; } = "Queued";
    /// <summary>Server file path for the generated PDF. 已生成 PDF 的服务器文件路径。</summary>
    public string? FilePath { get; set; }
    /// <summary>UTC timestamp when export was requested. 请求导出时的 UTC 时间戳。</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>UTC timestamp when export completed. 导出完成时的 UTC 时间戳。</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Failure message safe to display in the UI. 可在 UI 中显示的失败信息。</summary>
    public string? ErrorMessage { get; set; }
}
