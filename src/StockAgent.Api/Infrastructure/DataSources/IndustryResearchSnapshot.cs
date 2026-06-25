namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>Industry profile and recent industry news for the researched ticker. 被研究股票的行业画像和近期行业新闻。</summary>
public sealed record IndustryResearchSnapshot(
    string Ticker,
    string CompanyName,
    string IndustryName,
    IReadOnlyList<string> Sectors,
    IReadOnlyList<string> Keywords,
    string Provider,
    DateTimeOffset RetrievedAt,
    IReadOnlyList<IndustryNewsItem> News);

/// <summary>One recent industry news item. 一条近期行业新闻。</summary>
public sealed record IndustryNewsItem(
    string Title,
    string Url,
    string Source,
    DateTimeOffset? PublishedAt,
    string Summary);
