namespace StockAgent.Api.Domain;

/// <summary>Compressed, citation-ready evidence extracted from one document chunk. 从单个文档块提取的压缩型、可引用证据。</summary>
public sealed class EvidenceCard
{
    /// <summary>Unique evidence identifier. 唯一证据标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source document identifier. 源文档标识符。</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Chunk identifier that produced this card. 生成此卡片的块标识符。</summary>
    public Guid DocumentChunkId { get; set; }
    /// <summary>Short factual claim extracted from the chunk. 从块中提取的简短事实性主张。</summary>
    public string Claim { get; set; } = string.Empty;
    /// <summary>Metric or topic associated with the claim. 与主张相关的指标或主题。</summary>
    public string? Metric { get; set; }
    /// <summary>Short quote or paraphrased snippet for display. 用于展示的短引文或转述片段。</summary>
    public string Snippet { get; set; } = string.Empty;
    /// <summary>Confidence score from 0 to 1. 0 到 1 的置信度分数。</summary>
    public decimal Confidence { get; set; }
    /// <summary>Relevance score from 0 to 1 for retrieval ranking. 用于检索排序的 0 到 1 相关性分数。</summary>
    public decimal Relevance { get; set; }
    /// <summary>Source publication date when available. 可用时的源发布日期。</summary>
    public DateTimeOffset? SourceDate { get; set; }
    /// <summary>Report section where the card is most useful. 该卡片最适用的报告章节。</summary>
    public string ReportSection { get; set; } = string.Empty;
}
