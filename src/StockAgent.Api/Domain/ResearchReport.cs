namespace StockAgent.Api.Domain;

/// <summary>Generated stock research report persisted for reading and PDF export.</summary>
public sealed class ResearchReport
{
    /// <summary>Unique report identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Report language such as zh-CN.</summary>
    public string Language { get; set; } = "zh-CN";
    /// <summary>Markdown report body.</summary>
    public string Markdown { get; set; } = string.Empty;
    /// <summary>HTML report body rendered from Markdown.</summary>
    public string Html { get; set; } = string.Empty;
    /// <summary>Serialized structured rating JSON.</summary>
    public string RatingJson { get; set; } = "{}";
    /// <summary>Data cutoff timestamp for the research report.</summary>
    public DateTimeOffset DataCutoffAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Model provider used to generate the report.</summary>
    public string ModelProvider { get; set; } = "deterministic";
    /// <summary>Model name used to generate the report.</summary>
    public string ModelName { get; set; } = "fake-analysis-v1";
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
