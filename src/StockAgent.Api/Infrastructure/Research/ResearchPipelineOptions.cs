namespace StockAgent.Api.Infrastructure.Research;

/// <summary>
/// Configuration values that bound first-version research pipeline behavior.
/// 约束首个版本研究流水线行为的配置值。
/// </summary>
public sealed class ResearchPipelineOptions
{
    /// <summary>Maximum number of public evidence documents to collect for one task. 单个任务最多收集的公开证据文档数。</summary>
    public int MaxEvidenceDocuments { get; set; } = 10;

    /// <summary>Maximum number of evidence cards allowed in a model context pack. 模型上下文包允许的最大证据卡数量。</summary>
    public int MaxEvidenceCards { get; set; } = 30;

    /// <summary>Default report language used when a request does not specify one. 请求未指定时使用的默认报告语言。</summary>
    public string DefaultLanguage { get; set; } = "zh-CN";
}
