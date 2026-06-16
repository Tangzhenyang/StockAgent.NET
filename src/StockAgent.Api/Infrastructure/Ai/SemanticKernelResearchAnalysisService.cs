using Microsoft.SemanticKernel;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// First-version analysis service that owns the Semantic Kernel boundary while returning deterministic output for tests.
/// </summary>
public sealed class SemanticKernelResearchAnalysisService(Kernel kernel, ILogger<SemanticKernelResearchAnalysisService> logger)
    : IResearchAnalysisService
{
    /// <inheritdoc />
    public Task<AiAnalysisResult> AnalyzeAsync(
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(kernel);
        logger.LogInformation(
            "Semantic Kernel boundary invoked for {Ticker} with {EvidenceCount} evidence cards.",
            marketData.Ticker,
            evidenceCards.Count);

        var score = marketData.PeRatio < 25 && marketData.RevenueGrowthPercent > 0 ? 76 : 62;
        var result = new AiAnalysisResult(
            score,
            "中等",
            marketData.PeRatio < 20 ? "估值相对合理" : "估值需要结合增长验证",
            $"{marketData.CompanyName} 基本面保持稳定，证据数量为 {evidenceCards.Count} 条。",
            ["收入增长延续", "利润率保持稳定", "监管和宏观需求未显著恶化"]);

        return Task.FromResult(result);
    }
}
