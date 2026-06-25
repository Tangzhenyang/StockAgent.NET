using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Request body for creating a stock research task.
/// 创建股票研究任务的请求体。
/// </summary>
/// <param name="Ticker">User-entered ticker such as 600519, 00700.HK, or 700. 用户输入的股票代码，例如 600519、00700.HK 或 700。</param>
/// <param name="Market">Optional market hint selected by the user. 用户选择的可选市场提示。</param>
/// <param name="Language">Optional report language. The first version defaults to zh-CN. 可选的报告语言。首个版本默认为 zh-CN。</param>
public sealed record CreateResearchTaskRequest(string Ticker, Market? Market, string? Language);

/// <summary>
/// Response returned after a research task is created or loaded.
/// 研究任务创建或加载后返回的响应。
/// </summary>
/// <param name="Id">Research task identifier. 研究任务标识符。</param>
/// <param name="Ticker">Normalized ticker such as 600519.SH or 00700.HK. 规范化后的股票代码，例如 600519.SH 或 00700.HK。</param>
/// <param name="Market">Resolved stock market. 已解析的股票市场。</param>
/// <param name="Status">Current durable task status. 当前持久化任务状态。</param>
/// <param name="ProgressPercent">Approximate progress from 0 to 100. 从 0 到 100 的近似进度。</param>
/// <param name="Language">Report language selected for this task. 此任务选择的报告语言。</param>
/// <param name="CreatedAt">UTC creation timestamp. UTC 创建时间戳。</param>
/// <param name="UpdatedAt">UTC last update timestamp. UTC 最后更新时间戳。</param>
public sealed record ResearchTaskResponse(
    Guid Id,
    string Ticker,
    Market Market,
    ResearchTaskStatus Status,
    int ProgressPercent,
    string Language,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Diagnostic response for one durable research pipeline step.
/// 单个持久化研究流水线步骤的诊断响应。
/// </summary>
/// <param name="Id">Step identifier. 步骤标识符。</param>
/// <param name="StepName">Pipeline stage name. 流水线阶段名称。</param>
/// <param name="Status">Step execution status. 步骤执行状态。</param>
/// <param name="RetryCount">Retry attempts used by the step. 步骤已使用的重试次数。</param>
/// <param name="StartedAt">UTC step start time. UTC 步骤开始时间。</param>
/// <param name="CompletedAt">UTC step completion time. UTC 步骤完成时间。</param>
/// <param name="DurationMs">Step duration in milliseconds when it can be calculated. 可计算时的步骤耗时毫秒数。</param>
/// <param name="InputSummary">Short input summary safe for UI display. 可安全展示在 UI 的输入摘要。</param>
/// <param name="OutputSummary">Short output summary safe for UI display. 可安全展示在 UI 的输出摘要。</param>
/// <param name="ErrorMessage">Failure details safe for UI display. 可安全展示在 UI 的失败详情。</param>
/// <param name="IsLongRunning">Whether the step is running longer than the diagnostic threshold. 步骤是否超过诊断阈值仍在运行。</param>
public sealed record ResearchStepResponse(
    Guid Id,
    ResearchStage StepName,
    StepStatus Status,
    int RetryCount,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    long? DurationMs,
    string? InputSummary,
    string? OutputSummary,
    string? ErrorMessage,
    bool IsLongRunning);

/// <summary>
/// Structured artifact response for one research pipeline step.
/// 单个研究流水线步骤的结构化产物响应。
/// </summary>
/// <param name="Id">Artifact identifier. 产物标识符。</param>
/// <param name="Stage">Pipeline stage that produced the artifact. 生成产物的流水线阶段。</param>
/// <param name="ArtifactType">Machine-readable artifact category. 机器可读的产物类别。</param>
/// <param name="Title">Human-readable title. 人类可读标题。</param>
/// <param name="Summary">Short display summary. 展示用简短摘要。</param>
/// <param name="JsonPayload">Sanitized JSON payload. 脱敏后的 JSON 载荷。</param>
/// <param name="CreatedAt">UTC creation timestamp. UTC 创建时间戳。</param>
public sealed record ResearchStepArtifactResponse(
    Guid Id,
    ResearchStage Stage,
    string ArtifactType,
    string Title,
    string? Summary,
    string JsonPayload,
    DateTimeOffset CreatedAt);
