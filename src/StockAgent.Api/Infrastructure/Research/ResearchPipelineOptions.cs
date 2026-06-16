namespace StockAgent.Api.Infrastructure.Research;

/// <summary>
/// Configuration values that bound first-version research pipeline behavior.
/// </summary>
public sealed class ResearchPipelineOptions
{
    /// <summary>Maximum number of public evidence documents to collect for one task.</summary>
    public int MaxEvidenceDocuments { get; set; } = 10;

    /// <summary>Maximum number of evidence cards allowed in a model context pack.</summary>
    public int MaxEvidenceCards { get; set; } = 30;

    /// <summary>Default report language used when a request does not specify one.</summary>
    public string DefaultLanguage { get; set; } = "zh-CN";
}
