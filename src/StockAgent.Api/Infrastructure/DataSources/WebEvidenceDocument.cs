namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Public source document collected from a web/search provider.
/// </summary>
/// <param name="Url">Source URL.</param>
/// <param name="Title">Source title.</param>
/// <param name="SourceType">Source category such as annual-report or news.</param>
/// <param name="PublishedAt">Publication timestamp when known.</param>
/// <param name="Text">Plain text extracted from the source.</param>
public sealed record WebEvidenceDocument(
    string Url,
    string Title,
    string SourceType,
    DateTimeOffset? PublishedAt,
    string Text);
