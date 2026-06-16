using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Selects bounded evidence packs for model calls according to explicit context limits.
/// </summary>
public sealed class ContextBudgetManager
{
    /// <summary>
    /// Returns the most relevant evidence cards while respecting the maximum card count.
    /// </summary>
    /// <param name="evidenceCards">Candidate evidence cards gathered for a task.</param>
    /// <param name="maxCards">Maximum number of cards allowed in the selected pack.</param>
    /// <returns>Evidence cards ordered by relevance and confidence.</returns>
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
