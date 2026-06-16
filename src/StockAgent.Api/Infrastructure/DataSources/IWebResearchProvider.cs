namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Finds public documents that can support a research report.
/// </summary>
public interface IWebResearchProvider
{
    /// <summary>Returns public evidence documents for a normalized ticker and company name.</summary>
    Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(string ticker, string companyName, CancellationToken cancellationToken);
}
