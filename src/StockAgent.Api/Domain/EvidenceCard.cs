namespace StockAgent.Api.Domain;

/// <summary>Compressed, citation-ready evidence extracted from one document chunk.</summary>
public sealed class EvidenceCard
{
    /// <summary>Unique evidence identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source document identifier.</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Chunk identifier that produced this card.</summary>
    public Guid DocumentChunkId { get; set; }
    /// <summary>Short factual claim extracted from the chunk.</summary>
    public string Claim { get; set; } = string.Empty;
    /// <summary>Metric or topic associated with the claim.</summary>
    public string? Metric { get; set; }
    /// <summary>Short quote or paraphrased snippet for display.</summary>
    public string Snippet { get; set; } = string.Empty;
    /// <summary>Confidence score from 0 to 1.</summary>
    public decimal Confidence { get; set; }
    /// <summary>Relevance score from 0 to 1 for retrieval ranking.</summary>
    public decimal Relevance { get; set; }
    /// <summary>Source publication date when available.</summary>
    public DateTimeOffset? SourceDate { get; set; }
    /// <summary>Report section where the card is most useful.</summary>
    public string ReportSection { get; set; } = string.Empty;
}
