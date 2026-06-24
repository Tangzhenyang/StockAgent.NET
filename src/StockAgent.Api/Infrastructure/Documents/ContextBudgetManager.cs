using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Selects bounded evidence packs for model calls according to explicit context limits.
/// 根据明确的上下文限制选择受限的证据包以供模型调用。
/// </summary>
public sealed class ContextBudgetManager
{
    /// <summary>
    /// Returns the most relevant evidence cards while respecting the maximum card count.
    /// 在遵守最大卡片数量限制的前提下返回最相关的证据卡。
    /// </summary>
    /// <param name="evidenceCards">为任务收集到的候选证据卡。</param>
    /// <param name="maxCards">所选证据包允许的最大卡片数量。</param>
    /// <returns>按相关性和置信度排序的证据卡。</returns>
    public IReadOnlyList<EvidenceCard> SelectEvidence(IEnumerable<EvidenceCard> evidenceCards, int maxCards)
    {
        if (maxCards <= 0)
        {
            return [];
        }

        return evidenceCards
            .OrderByDescending(x => x.Relevance)
            .ThenByDescending(x => x.Confidence)
            .Take(maxCards)
            .ToList();
    }
}
