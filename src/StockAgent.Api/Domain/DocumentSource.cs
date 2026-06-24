namespace StockAgent.Api.Domain;

/// <summary>Original source document or web page collected for a research task. 为研究任务收集的原始源文档或网页。</summary>
public sealed class DocumentSource
{
    /// <summary>Unique source identifier. 唯一源标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source URL when available. 可用时的源 URL。</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Human-readable source title. 人类可读的源标题。</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Source type such as filing, report, news, or company-site. 源类型，例如公告、报告、新闻或公司网站。</summary>
    public string SourceType { get; set; } = string.Empty;
    /// <summary>Publisher or host name. 发布者或主机名称。</summary>
    public string? Publisher { get; set; }
    /// <summary>Original publication timestamp when known. 已知时的原始发布时间戳。</summary>
    public DateTimeOffset? PublishedAt { get; set; }
    /// <summary>UTC timestamp when the system retrieved the source. 系统检索源时的 UTC 时间戳。</summary>
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Stable hash used for de-duplication. 用于去重的稳定哈希。</summary>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>Path to stored raw content. 已存储原始内容的路径。</summary>
    public string? RawContentPath { get; set; }
    /// <summary>Path to parsed text content. 已解析文本内容的路径。</summary>
    public string? ParsedContentPath { get; set; }
}
