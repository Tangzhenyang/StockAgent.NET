namespace StockAgent.Api.Domain;

/// <summary>
/// Root entity for a user-submitted stock research workflow.
/// </summary>
public sealed class ResearchTask
{
    /// <summary>Unique task identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Normalized ticker, such as 600519.SH or 00700.HK.</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>Supported market for the ticker.</summary>
    public Market Market { get; set; }
    /// <summary>Company name when known from data providers.</summary>
    public string? CompanyName { get; set; }
    /// <summary>Durable lifecycle state.</summary>
    public ResearchTaskStatus Status { get; set; } = ResearchTaskStatus.Queued;
    /// <summary>Current pipeline stage, if the task has started.</summary>
    public ResearchStage? CurrentStage { get; set; }
    /// <summary>Approximate task progress from 0 to 100.</summary>
    public int ProgressPercent { get; set; }
    /// <summary>Latest task-level failure message.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Report language. The MVP defaults to zh-CN.</summary>
    public string Language { get; set; } = "zh-CN";
    /// <summary>Creation timestamp in UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Last update timestamp in UTC.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Step records for this task.</summary>
    public List<ResearchStep> Steps { get; set; } = [];
}
