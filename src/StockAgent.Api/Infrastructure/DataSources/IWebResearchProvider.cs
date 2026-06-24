using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Finds public documents that can support a research report.
/// 查找可支持研究报告的公开文档。
/// </summary>
public interface IWebResearchProvider
{
    /// <summary>Returns public evidence documents using the user's data source settings. 使用用户数据源设置返回公开证据文档。</summary>
    Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(
        string ticker,
        string companyName,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken);
}
