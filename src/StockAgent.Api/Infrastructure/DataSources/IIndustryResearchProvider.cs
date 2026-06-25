using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>Fetches industry profile and recent industry news. 获取行业画像和近期行业新闻。</summary>
public interface IIndustryResearchProvider
{
    /// <summary>Returns industry profile using user data source settings. 使用用户数据源设置返回行业画像。</summary>
    Task<IndustryResearchSnapshot> GetIndustryAsync(
        string ticker,
        string companyName,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken);
}
