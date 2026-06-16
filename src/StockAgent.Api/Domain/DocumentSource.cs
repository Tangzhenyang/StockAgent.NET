namespace StockAgent.Api.Domain;

/// <summary>Original source document or web page collected for a research task.</summary>
public sealed class DocumentSource
{
    /// <summary>Unique source identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source URL when available.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Human-readable source title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Source type such as filing, report, news, or company-site.</summary>
    public string SourceType { get; set; } = string.Empty;
    /// <summary>Publisher or host name.</summary>
    public string? Publisher { get; set; }
    /// <summary>Original publication timestamp when known.</summary>
    public DateTimeOffset? PublishedAt { get; set; }
    /// <summary>UTC timestamp when the system retrieved the source.</summary>
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Stable hash used for de-duplication.</summary>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>Path to stored raw content.</summary>
    public string? RawContentPath { get; set; }
    /// <summary>Path to parsed text content.</summary>
    public string? ParsedContentPath { get; set; }
}
