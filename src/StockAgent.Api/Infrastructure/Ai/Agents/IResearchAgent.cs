namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Typed boundary for a single role-specific research agent.
/// 单个角色型研究 Agent 的强类型边界。
/// </summary>
/// <typeparam name="TInput">Agent input contract. Agent 输入契约。</typeparam>
/// <typeparam name="TOutput">Agent output contract. Agent 输出契约。</typeparam>
public interface IResearchAgent<in TInput, TOutput>
{
    /// <summary>Agent display name used in logs and model invocation records. 用于日志和模型调用记录的 Agent 名称。</summary>
    string Name { get; }

    /// <summary>Runs the agent for one research subtask. 为一个研究子任务运行 Agent。</summary>
    Task<TOutput> RunAsync(TInput input, CancellationToken cancellationToken);
}
