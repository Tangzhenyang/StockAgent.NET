using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Request body for creating a stock research task.
/// </summary>
/// <param name="Ticker">User-entered ticker such as 600519, 00700.HK, or 700.</param>
/// <param name="Market">Optional market hint selected by the user.</param>
/// <param name="Language">Optional report language. The first version defaults to zh-CN.</param>
public sealed record CreateResearchTaskRequest(string Ticker, Market? Market, string? Language);

/// <summary>
/// Response returned after a research task is created or loaded.
/// </summary>
/// <param name="Id">Research task identifier.</param>
/// <param name="Ticker">Normalized ticker such as 600519.SH or 00700.HK.</param>
/// <param name="Market">Resolved stock market.</param>
/// <param name="Status">Current durable task status.</param>
/// <param name="ProgressPercent">Approximate progress from 0 to 100.</param>
/// <param name="Language">Report language selected for this task.</param>
public sealed record ResearchTaskResponse(
    Guid Id,
    string Ticker,
    Market Market,
    ResearchTaskStatus Status,
    int ProgressPercent,
    string Language);
