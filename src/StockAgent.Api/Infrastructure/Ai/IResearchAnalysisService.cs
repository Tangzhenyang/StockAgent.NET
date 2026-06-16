using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Boundary for Semantic Kernel-backed research analysis over bounded evidence packs.
/// </summary>
public interface IResearchAnalysisService
{
    /// <summary>
    /// Analyzes structured market data and selected evidence cards.
    /// </summary>
    /// <param name="marketData">Structured market and financial snapshot.</param>
    /// <param name="evidenceCards">Bounded evidence cards selected for the model call.</param>
    /// <param name="cancellationToken">Cancellation token for request shutdown.</param>
    /// <returns>Structured analysis result for scoring and report generation.</returns>
    Task<AiAnalysisResult> AnalyzeAsync(
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        CancellationToken cancellationToken);
}
