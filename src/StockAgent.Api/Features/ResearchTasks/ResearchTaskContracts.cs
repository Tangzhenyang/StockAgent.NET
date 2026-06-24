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
