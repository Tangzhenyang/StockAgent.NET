namespace StockAgent.Api.Features.UserSettings;

/// <summary>
/// Request body for saving the user's model provider configuration.
/// 保存用户模型提供商配置的请求体。
/// </summary>
/// <param name="Provider">Provider kind, such as OpenAICompatible. 提供商类型，例如 OpenAICompatible。</param>
/// <param name="BaseUrl">Model API base URL. 模型 API 基础地址。</param>
/// <param name="Model">Model name. 模型名称。</param>
/// <param name="ApiKey">Optional replacement API key. 可选的替换 API Key。</param>
public sealed record SaveModelSettingsRequest(string Provider, string BaseUrl, string Model, string? ApiKey);

/// <summary>
/// Sanitized model configuration returned to the browser.
/// 返回给浏览器的脱敏模型配置。
/// </summary>
/// <param name="Provider">Provider kind. 提供商类型。</param>
/// <param name="BaseUrl">Model API base URL. 模型 API 基础地址。</param>
/// <param name="Model">Model name. 模型名称。</param>
/// <param name="ApiKeyConfigured">Whether an API key exists for the user. 用户是否已配置 API Key。</param>
/// <param name="UpdatedAt">UTC timestamp when the configuration was last changed. 配置最后变更的 UTC 时间戳。</param>
public sealed record ModelSettingsResponse(
    string Provider,
    string BaseUrl,
    string Model,
    bool ApiKeyConfigured,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Backend-only model settings with decrypted API key.
/// 包含解密 API Key 的后端专用模型配置。
/// </summary>
/// <param name="Provider">Provider kind, such as OpenAICompatible. 提供商类型，例如 OpenAICompatible。</param>
/// <param name="BaseUrl">Model API base URL. 模型 API 基础地址。</param>
/// <param name="Model">Model name. 模型名称。</param>
/// <param name="ApiKey">Decrypted model API key. 解密后的模型 API Key。</param>
public sealed record ModelRuntimeSettings(string Provider, string BaseUrl, string Model, string? ApiKey)
{
    /// <summary>
    /// Returns whether the settings are complete enough for a real model call.
    /// 返回配置是否足以进行真实模型调用。
    /// </summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Provider)
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Model)
        && !string.IsNullOrWhiteSpace(ApiKey);
}

/// <summary>
/// Request body for saving research pipeline limits.
/// 保存研究流水线限制的请求体。
/// </summary>
/// <param name="DefaultLanguage">Default report language. 默认报告语言。</param>
/// <param name="MaxEvidenceCards">Maximum evidence cards sent to analysis. 送入分析的最大证据卡数量。</param>
/// <param name="MaxDocumentChunks">Maximum chunks retained per task. 每个任务保留的最大文档分块数量。</param>
/// <param name="MaxRetrievedChunks">Maximum chunks retrieved into model context. 检索进入模型上下文的最大分块数量。</param>
/// <param name="RetainRawDocuments">Whether raw source text should be retained. 是否保留原始源文本。</param>
public sealed record SaveResearchSettingsRequest(
    string DefaultLanguage,
    int MaxEvidenceCards,
    int MaxDocumentChunks,
    int MaxRetrievedChunks,
    bool RetainRawDocuments);

/// <summary>
/// Research configuration returned to the browser and pipeline.
/// 返回给浏览器和流水线的研究配置。
/// </summary>
/// <param name="DefaultLanguage">Default report language. 默认报告语言。</param>
/// <param name="MaxEvidenceCards">Maximum evidence cards sent to analysis. 送入分析的最大证据卡数量。</param>
/// <param name="MaxDocumentChunks">Maximum chunks retained per task. 每个任务保留的最大文档分块数量。</param>
/// <param name="MaxRetrievedChunks">Maximum chunks retrieved into model context. 检索进入模型上下文的最大分块数量。</param>
/// <param name="RetainRawDocuments">Whether raw source text should be retained. 是否保留原始源文本。</param>
public sealed record ResearchSettingsResponse(
    string DefaultLanguage,
    int MaxEvidenceCards,
    int MaxDocumentChunks,
    int MaxRetrievedChunks,
    bool RetainRawDocuments);

/// <summary>
/// Request body for saving external data source configuration.
/// 保存外部数据源配置的请求体。
/// </summary>
/// <param name="OfficialAnnouncementsEnabled">Whether official announcement sources should be used. 是否启用官方公告源。</param>
/// <param name="NewsSearchEnabled">Whether news/search sources should be used. 是否启用新闻/搜索源。</param>
/// <param name="MarketDataProvider">Market provider kind, such as Mock or CustomHttp. 行情数据提供器类型，例如 Mock 或 CustomHttp。</param>
/// <param name="MarketDataBaseUrl">Market provider base URL. 行情数据提供器基础地址。</param>
/// <param name="MarketDataApiKey">Optional replacement market data API key. 可选的行情数据 API Key。</param>
/// <param name="WebResearchProvider">Web research provider kind, such as Mock or CustomHttp. 网页研究提供器类型，例如 Mock 或 CustomHttp。</param>
/// <param name="WebResearchBaseUrl">Web research provider base URL. 网页研究提供器基础地址。</param>
/// <param name="WebResearchApiKey">Optional replacement web research API key. 可选的网页研究 API Key。</param>
/// <param name="MaxRequestsPerMinute">Per-user request limit used by providers. 提供器使用的每用户每分钟请求上限。</param>
/// <param name="RetryCount">Provider retry count. 提供器重试次数。</param>
public sealed record SaveDataSourceSettingsRequest(
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

/// <summary>
/// Sanitized external data source configuration returned to the browser.
/// 返回给浏览器的脱敏外部数据源配置。
/// </summary>
/// <param name="OfficialAnnouncementsEnabled">Whether official announcement sources are enabled. 是否启用官方公告源。</param>
/// <param name="NewsSearchEnabled">Whether news/search sources are enabled. 是否启用新闻/搜索源。</param>
/// <param name="MarketDataProvider">Market provider kind. 行情数据提供器类型。</param>
/// <param name="MarketDataBaseUrl">Market provider base URL. 行情数据提供器基础地址。</param>
/// <param name="MarketDataApiKeyConfigured">Whether a market provider API key exists. 是否已配置行情数据 API Key。</param>
/// <param name="WebResearchProvider">Web research provider kind. 网页研究提供器类型。</param>
/// <param name="WebResearchBaseUrl">Web research provider base URL. 网页研究提供器基础地址。</param>
/// <param name="WebResearchApiKeyConfigured">Whether a web research API key exists. 是否已配置网页研究 API Key。</param>
/// <param name="MaxRequestsPerMinute">Per-user request limit used by providers. 提供器使用的每用户每分钟请求上限。</param>
/// <param name="RetryCount">Provider retry count. 提供器重试次数。</param>
/// <param name="UpdatedAt">UTC timestamp when the configuration was last changed. 配置最后变更的 UTC 时间戳。</param>
public sealed record DataSourceSettingsResponse(
    bool OfficialAnnouncementsEnabled,
    bool NewsSearchEnabled,
    string MarketDataProvider,
    string MarketDataBaseUrl,
    bool MarketDataApiKeyConfigured,
    string WebResearchProvider,
    string WebResearchBaseUrl,
    bool WebResearchApiKeyConfigured,
    int MaxRequestsPerMinute,
    int RetryCount,
    DateTimeOffset? UpdatedAt);

/// <summary>
/// Combined user settings payload.
/// 用户设置组合载荷。
/// </summary>
/// <param name="Model">Sanitized model configuration. 脱敏模型配置。</param>
/// <param name="Research">Research configuration. 研究配置。</param>
/// <param name="DataSources">Sanitized data source configuration. 脱敏数据源配置。</param>
public sealed record UserSettingsResponse(
    ModelSettingsResponse Model,
    ResearchSettingsResponse Research,
    DataSourceSettingsResponse DataSources);

/// <summary>
/// Lightweight model configuration validation response.
/// 轻量模型配置验证响应。
/// </summary>
/// <param name="Succeeded">Whether the saved model configuration is complete. 已保存模型配置是否完整。</param>
/// <param name="Message">Readable validation message. 可读验证信息。</param>
public sealed record ModelSettingsTestResponse(bool Succeeded, string Message);

/// <summary>
/// Lightweight data source configuration validation response.
/// 轻量数据源配置验证响应。
/// </summary>
/// <param name="Succeeded">Whether the saved data source configuration is complete. 已保存数据源配置是否完整。</param>
/// <param name="Message">Readable validation message. 可读验证信息。</param>
public sealed record DataSourceSettingsTestResponse(bool Succeeded, string Message);
