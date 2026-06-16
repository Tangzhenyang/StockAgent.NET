namespace StockAgent.Api.Domain;

/// <summary>PDF export audit record for a research report.</summary>
public sealed class PdfExport
{
    /// <summary>Unique PDF export identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Export status such as Queued, Running, Completed, or Failed.</summary>
    public string Status { get; set; } = "Queued";
    /// <summary>Server file path for the generated PDF.</summary>
    public string? FilePath { get; set; }
    /// <summary>UTC timestamp when export was requested.</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>UTC timestamp when export completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Failure message safe to display in the UI.</summary>
    public string? ErrorMessage { get; set; }
}
