namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Deterministic web research provider that returns representative public evidence documents.
/// </summary>
public sealed class FakeWebResearchProvider : IWebResearchProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(string ticker, string companyName, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebEvidenceDocument> documents =
        [
            new WebEvidenceDocument(
                "https://example.local/annual-report",
                $"{companyName} 年报摘要",
                "annual-report",
                DateTimeOffset.UtcNow.AddMonths(-3),
                $"{companyName} 收入保持增长，经营利润率稳定，现金流表现稳健。管理层提示宏观需求和监管变化是主要风险。"),
            new WebEvidenceDocument(
                "https://example.local/news",
                $"{companyName} 业务进展",
                "news",
                DateTimeOffset.UtcNow.AddDays(-14),
                $"{companyName} 核心业务保持用户规模优势，新业务投入仍影响短期利润率。")
        ];

        return Task.FromResult(documents);
    }
}
