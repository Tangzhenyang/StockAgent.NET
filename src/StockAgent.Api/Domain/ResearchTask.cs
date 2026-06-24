namespace StockAgent.Api.Domain;

/// <summary>
/// Root entity for a user-submitted stock research workflow.
/// 用户提交的股票研究工作流根实体。
/// </summary>
public sealed class ResearchTask
{
    /// <summary>Unique task identifier. 唯一任务标识符。</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Owner user identifier from ASP.NET Core Identity. ASP.NET Core Identity 中的所属用户标识符。</summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>Owner user navigation property. 所属用户导航属性。</summary>
    public ApplicationUser? User { get; set; }
    /// <summary>Normalized ticker, such as 600519.SH or 00700.HK. 规范化后的股票代码，例如 600519.SH 或 00700.HK。</summary>
    public string Ticker { get; set; } = string.Empty;
    /// <summary>Supported market for the ticker. 该股票代码对应的支持市场。</summary>
    public Market Market { get; set; }
    /// <summary>Company name when known from data providers. 数据提供器已知时的公司名称。</summary>
    public string? CompanyName { get; set; }
    /// <summary>Durable lifecycle state. 持久化生命周期状态。</summary>
    public ResearchTaskStatus Status { get; set; } = ResearchTaskStatus.Queued;
    /// <summary>Current pipeline stage, if the task has started. 任务启动后的当前流水线阶段。</summary>
    public ResearchStage? CurrentStage { get; set; }
    /// <summary>Approximate task progress from 0 to 100. 0 到 100 的近似任务进度。</summary>
    public int ProgressPercent { get; set; }
    /// <summary>Latest task-level failure message. 最近一次任务级失败信息。</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>Report language. The MVP defaults to zh-CN. 报告语言。MVP 默认值为 zh-CN。</summary>
    public string Language { get; set; } = "zh-CN";
    /// <summary>Creation timestamp in UTC. UTC 创建时间戳。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Last update timestamp in UTC. UTC 最后更新时间戳。</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Step records for this task. 该任务的步骤记录。</summary>
    public List<ResearchStep> Steps { get; set; } = [];
}
