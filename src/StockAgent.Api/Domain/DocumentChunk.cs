namespace StockAgent.Api.Domain;

/// <summary>Bounded text block derived from a source document.</summary>
public sealed class DocumentChunk
{
    /// <summary>Unique chunk identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent source document identifier.</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Zero-based chunk index within the source.</summary>
    public int ChunkIndex { get; set; }
    /// <summary>Page number for PDF-derived chunks when available.</summary>
    public int? PageNumber { get; set; }
    /// <summary>Section heading near the chunk when available.</summary>
    public string? SectionTitle { get; set; }
    /// <summary>Chunk text sent through retrieval and summarization.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Rough token estimate for context budgeting.</summary>
    public int TokenEstimate { get; set; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
