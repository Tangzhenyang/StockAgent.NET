using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Documents;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies that evidence packs are capped before model calls.
/// 验证证据包在模型调用前会被限制大小。
/// </summary>
public sealed class ContextBudgetManagerTests
{
    /// <summary>
    /// The highest relevance evidence cards are kept within the requested limit.
    /// 保留相关性最高的证据卡，并控制在请求的限制内。
    /// </summary>
    [Fact]
    public void SelectEvidence_KeepsHighestRelevanceCards()
    {
        var manager = new ContextBudgetManager();
        var cards = Enumerable.Range(1, 10)
            .Select(i => new EvidenceCard { Claim = $"claim-{i}", Relevance = i / 10m, Snippet = "snippet", ReportSection = "Risk" })
            .ToList();

        var selected = manager.SelectEvidence(cards, maxCards: 3);

        selected.Select(x => x.Claim).Should().Equal("claim-10", "claim-9", "claim-8");
    }
}
