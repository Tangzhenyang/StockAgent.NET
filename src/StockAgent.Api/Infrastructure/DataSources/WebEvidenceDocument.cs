namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Public source document collected from a web/search provider.
/// 从网页/搜索提供器收集的公开源文档。
/// </summary>
/// <param name="Url">Source URL. 源地址。</param>
/// <param name="Title">Source title. 源标题。</param>
/// <param name="SourceType">Source category such as annual-report or news. 源类别，例如 annual-report 或 news。</param>
/// <param name="PublishedAt">Publication timestamp when known. 已知时的发布时间戳。</param>
/// <param name="Text">Plain text extracted from the source. 从源中提取的纯文本。</param>
public sealed record WebEvidenceDocument(
    string Url,
    string Title,
    string SourceType,
    DateTimeOffset? PublishedAt,
    string Text);
