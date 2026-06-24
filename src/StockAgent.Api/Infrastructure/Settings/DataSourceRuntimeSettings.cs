namespace StockAgent.Api.Infrastructure.Settings;

/// <summary>
/// Backend-only data source settings with unprotected provider keys for runtime calls.
/// 仅后端使用的数据源运行时设置，包含供调用使用的解密提供器密钥。
/// </summary>
/// <param name="OfficialAnnouncementsEnabled">Whether official announcement sources should be used. 是否启用官方公告源。</param>
/// <param name="NewsSearchEnabled">Whether news/search sources should be used. 是否启用新闻/搜索源。</param>
/// <param name="MarketDataProvider">Market provider kind. 行情数据提供器类型。</param>
/// <param name="MarketDataBaseUrl">Market provider base URL. 行情数据提供器基础地址。</param>
/// <param name="MarketDataApiKey">Optional market provider API key. 可选的行情数据 API Key。</param>
/// <param name="WebResearchProvider">Web research provider kind. 网页研究提供器类型。</param>
/// <param name="WebResearchBaseUrl">Web research provider base URL. 网页研究提供器基础地址。</param>
/// <param name="WebResearchApiKey">Optional web research provider API key. 可选的网页研究 API Key。</param>
/// <param name="MaxRequestsPerMinute">Per-user request limit used by providers. 提供器使用的每用户每分钟请求上限。</param>
/// <param name="RetryCount">Provider retry count. 提供器重试次数。</param>
public sealed record DataSourceRuntimeSettings(
    bool OfficialAnnouncementsEnabled,
    bool NewsSearchEnabled,
    string MarketDataProvider,
    string MarketDataBaseUrl,
    string? MarketDataApiKey,
    string WebResearchProvider,
    string WebResearchBaseUrl,
    string? WebResearchApiKey,
    int MaxRequestsPerMinute,
    int RetryCount);
