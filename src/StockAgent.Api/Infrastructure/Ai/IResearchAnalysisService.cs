using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Boundary for Semantic Kernel-backed research analysis over bounded evidence packs.
/// 基于受限证据包的 Semantic Kernel 研究分析边界。
/// </summary>
public interface IResearchAnalysisService
{
    /// <summary>Runs the fixed-flow multi-agent analysis. 运行固定流程多 Agent 分析。</summary>
    /// <param name="researchTaskId">Research task identifier used for invocation audit records. 用于调用审计记录的研究任务标识。</param>
    /// <param name="marketData">Structured market and financial snapshot. 结构化的市场和财务快照。</param>
    /// <param name="evidenceCards">Bounded evidence cards selected for the model call. 为模型调用选择的受限证据卡。</param>
    /// <param name="modelSettings">Runtime model settings with decrypted API key. 包含解密 API Key 的运行时模型设置。</param>
    /// <param name="language">Report and analysis language. 报告和分析语言。</param>
    /// <param name="cancellationToken">Cancellation token for request shutdown. 用于请求中止的取消令牌。</param>
    /// <param name="industryData">Optional industry profile and recent sector news. 可选行业画像和近期赛道消息。</param>
    /// <returns>Structured analysis result for scoring and report generation. 用于评分和报告生成的结构化分析结果。</returns>
    Task<AiAnalysisResult> AnalyzeAsync(
        Guid researchTaskId,
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        ModelRuntimeSettings modelSettings,
        string language,
        CancellationToken cancellationToken,
        IndustryResearchSnapshot? industryData = null);
}
