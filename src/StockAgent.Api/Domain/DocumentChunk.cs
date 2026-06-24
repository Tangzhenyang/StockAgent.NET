namespace StockAgent.Api.Domain;

/// <summary>Bounded text block derived from a source document. 派生自源文档的受限文本块。</summary>
public sealed class DocumentChunk
{
    /// <summary>Unique chunk identifier. 唯一块标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent source document identifier. 父级源文档标识符。</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Zero-based chunk index within the source. 源文档中从零开始的块索引。</summary>
    public int ChunkIndex { get; set; }
    /// <summary>Page number for PDF-derived chunks when available. PDF 派生块可用时的页码。</summary>
    public int? PageNumber { get; set; }
    /// <summary>Section heading near the chunk when available. 块附近可用时的章节标题。</summary>
    public string? SectionTitle { get; set; }
    /// <summary>Chunk text sent through retrieval and summarization. 用于检索和摘要的块文本。</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Rough token estimate for context budgeting. 用于上下文预算的粗略 token 估算。</summary>
    public int TokenEstimate { get; set; }
    /// <summary>UTC creation timestamp. UTC 创建时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
