namespace StockAgent.Api.Domain;

/// <summary>Generated stock research report persisted for reading and PDF export. 为阅读和 PDF 导出而持久化的股票研究报告。</summary>
public sealed class ResearchReport
{
    /// <summary>Unique report identifier. 唯一报告标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier. 父级研究任务标识符。</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Report language such as zh-CN. 报告语言，例如 zh-CN。</summary>
    public string Language { get; set; } = "zh-CN";
    /// <summary>Markdown report body. Markdown 报告正文。</summary>
    public string Markdown { get; set; } = string.Empty;
    /// <summary>HTML report body rendered from Markdown. 由 Markdown 渲染生成的 HTML 报告正文。</summary>
    public string Html { get; set; } = string.Empty;
    /// <summary>Serialized structured rating JSON. 序列化后的结构化评分 JSON。</summary>
    public string RatingJson { get; set; } = "{}";
    /// <summary>Data cutoff timestamp for the research report. 研究报告的数据截止时间戳。</summary>
    public DateTimeOffset DataCutoffAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Model provider used to generate the report. 用于生成报告的模型提供器。</summary>
    public string ModelProvider { get; set; } = "deterministic";
    /// <summary>Model name used to generate the report. 用于生成报告的模型名称。</summary>
    public string ModelName { get; set; } = "fake-analysis-v1";
    /// <summary>UTC creation timestamp. UTC 创建时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
