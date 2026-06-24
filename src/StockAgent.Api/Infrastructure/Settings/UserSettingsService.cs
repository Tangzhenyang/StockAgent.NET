using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Security;

namespace StockAgent.Api.Infrastructure.Settings;

/// <summary>
/// Reads and writes sanitized current-user settings while keeping API keys protected.
/// 读取和写入脱敏后的当前用户设置，同时保护 API Key。
/// </summary>
public sealed class UserSettingsService(StockAgentDbContext db, IApiKeyProtector apiKeyProtector)
{
    private const string ModelKey = "model";
    private const string ResearchKey = "research";
    private const string DataSourcesKey = "dataSources";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Gets the combined settings payload for a user. 获取用户的组合设置载荷。</summary>
    public async Task<UserSettingsResponse> GetSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var model = await GetModelSettingsAsync(userId, cancellationToken);
        var research = await GetResearchSettingsAsync(userId, cancellationToken);
        var dataSources = await GetDataSourceSettingsAsync(userId, cancellationToken);
        return new UserSettingsResponse(model, research, dataSources);
    }

    /// <summary>Gets sanitized model settings for a user. 获取用户的脱敏模型设置。</summary>
    public async Task<ModelSettingsResponse> GetModelSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var stored = await LoadModelSettingsAsync(userId, cancellationToken);
        return ToResponse(stored);
    }

    /// <summary>Gets backend-only model settings with unprotected API key. 获取包含解密密钥的后端专用模型设置。</summary>
    public async Task<ModelRuntimeSettings> GetModelRuntimeSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var stored = await LoadModelSettingsAsync(userId, cancellationToken);
        return new ModelRuntimeSettings(
            stored.Provider,
            stored.BaseUrl,
            stored.Model,
            UnprotectOptional(stored.EncryptedApiKey));
    }

    /// <summary>Gets research settings for a user, falling back to defaults. 获取用户研究设置，不存在时返回默认值。</summary>
    public async Task<ResearchSettingsResponse> GetResearchSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var setting = await FindSettingAsync(userId, ResearchKey, cancellationToken);
        if (setting is null)
        {
            return CreateDefaultResearchSettings();
        }

        return JsonSerializer.Deserialize<ResearchSettingsResponse>(setting.SettingValueJson, JsonOptions)
               ?? CreateDefaultResearchSettings();
    }

    /// <summary>Gets sanitized data source settings for a user. 获取用户的脱敏数据源设置。</summary>
    public async Task<DataSourceSettingsResponse> GetDataSourceSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var stored = await LoadDataSourceSettingsAsync(userId, cancellationToken);
        return ToResponse(stored);
    }

    /// <summary>Gets backend-only data source settings with unprotected keys. 获取包含解密密钥的后端专用数据源设置。</summary>
    public async Task<DataSourceRuntimeSettings> GetDataSourceRuntimeSettingsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var stored = await LoadDataSourceSettingsAsync(userId, cancellationToken);
        return new DataSourceRuntimeSettings(
            stored.OfficialAnnouncementsEnabled,
            stored.NewsSearchEnabled,
            stored.MarketDataProvider,
            stored.MarketDataBaseUrl,
            UnprotectOptional(stored.EncryptedMarketDataApiKey),
            stored.WebResearchProvider,
            stored.WebResearchBaseUrl,
            UnprotectOptional(stored.EncryptedWebResearchApiKey),
            stored.MaxRequestsPerMinute,
            stored.RetryCount);
    }

    /// <summary>Saves model settings and optionally replaces the protected API key. 保存模型设置并可选替换受保护的 API Key。</summary>
    public async Task<ModelSettingsResponse> SaveModelSettingsAsync(
        string userId,
        SaveModelSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await LoadModelSettingsAsync(userId, cancellationToken);
        var protectedApiKey = string.IsNullOrWhiteSpace(request.ApiKey)
            ? existing.EncryptedApiKey
            : apiKeyProtector.Protect(request.ApiKey.Trim());

        var stored = new StoredModelSettings(
            request.Provider.Trim(),
            request.BaseUrl.Trim(),
            request.Model.Trim(),
            protectedApiKey,
            DateTimeOffset.UtcNow);
        await UpsertSettingAsync(userId, ModelKey, stored, cancellationToken);
        return ToResponse(stored);
    }

    /// <summary>Saves research settings for the user. 保存用户研究设置。</summary>
    public async Task<ResearchSettingsResponse> SaveResearchSettingsAsync(
        string userId,
        SaveResearchSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var settings = new ResearchSettingsResponse(
            request.DefaultLanguage.Trim(),
            request.MaxEvidenceCards,
            request.MaxDocumentChunks,
            request.MaxRetrievedChunks,
            request.RetainRawDocuments);
        await UpsertSettingAsync(userId, ResearchKey, settings, cancellationToken);
        return settings;
    }

    /// <summary>Saves data source settings and optionally replaces protected provider keys. 保存数据源设置并可选替换受保护的提供器密钥。</summary>
    public async Task<DataSourceSettingsResponse> SaveDataSourceSettingsAsync(
        string userId,
        SaveDataSourceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await LoadDataSourceSettingsAsync(userId, cancellationToken);
        var marketApiKey = string.IsNullOrWhiteSpace(request.MarketDataApiKey)
            ? existing.EncryptedMarketDataApiKey
            : apiKeyProtector.Protect(request.MarketDataApiKey.Trim());
        var webApiKey = string.IsNullOrWhiteSpace(request.WebResearchApiKey)
            ? existing.EncryptedWebResearchApiKey
            : apiKeyProtector.Protect(request.WebResearchApiKey.Trim());

        var stored = new StoredDataSourceSettings(
            request.OfficialAnnouncementsEnabled,
            request.NewsSearchEnabled,
            NormalizeProvider(request.MarketDataProvider),
            request.MarketDataBaseUrl.Trim(),
            marketApiKey,
            NormalizeProvider(request.WebResearchProvider),
            request.WebResearchBaseUrl.Trim(),
            webApiKey,
            request.MaxRequestsPerMinute,
            request.RetryCount,
            DateTimeOffset.UtcNow);
        await UpsertSettingAsync(userId, DataSourcesKey, stored, cancellationToken);
        return ToResponse(stored);
    }

    private async Task<StoredModelSettings> LoadModelSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var setting = await FindSettingAsync(userId, ModelKey, cancellationToken);
        if (setting is null)
        {
            return StoredModelSettings.Empty;
        }

        return JsonSerializer.Deserialize<StoredModelSettings>(setting.SettingValueJson, JsonOptions)
               ?? StoredModelSettings.Empty;
    }

    private async Task<StoredDataSourceSettings> LoadDataSourceSettingsAsync(string userId, CancellationToken cancellationToken)
    {
        var setting = await FindSettingAsync(userId, DataSourcesKey, cancellationToken);
        if (setting is null)
        {
            return StoredDataSourceSettings.Default;
        }

        return JsonSerializer.Deserialize<StoredDataSourceSettings>(setting.SettingValueJson, JsonOptions)
               ?? StoredDataSourceSettings.Default;
    }

    private async Task<UserSetting?> FindSettingAsync(string userId, string key, CancellationToken cancellationToken)
    {
        return await db.UserSettings.FirstOrDefaultAsync(
            x => x.UserId == userId && x.SettingKey == key,
            cancellationToken);
    }

    private async Task UpsertSettingAsync<TValue>(
        string userId,
        string key,
        TValue value,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var setting = await FindSettingAsync(userId, key, cancellationToken);
        if (setting is null)
        {
            db.UserSettings.Add(new UserSetting
            {
                UserId = userId,
                SettingKey = key,
                SettingValueJson = json,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            setting.SettingValueJson = json;
            setting.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static ModelSettingsResponse ToResponse(StoredModelSettings stored)
    {
        return new ModelSettingsResponse(
            stored.Provider,
            stored.BaseUrl,
            stored.Model,
            !string.IsNullOrWhiteSpace(stored.EncryptedApiKey),
            stored.UpdatedAt);
    }

    private static DataSourceSettingsResponse ToResponse(StoredDataSourceSettings stored)
    {
        return new DataSourceSettingsResponse(
            stored.OfficialAnnouncementsEnabled,
            stored.NewsSearchEnabled,
            stored.MarketDataProvider,
            stored.MarketDataBaseUrl,
            !string.IsNullOrWhiteSpace(stored.EncryptedMarketDataApiKey),
            stored.WebResearchProvider,
            stored.WebResearchBaseUrl,
            !string.IsNullOrWhiteSpace(stored.EncryptedWebResearchApiKey),
            stored.MaxRequestsPerMinute,
            stored.RetryCount,
            stored.UpdatedAt);
    }

    private static ResearchSettingsResponse CreateDefaultResearchSettings()
    {
        return new ResearchSettingsResponse("zh-CN", 30, 300, 30, false);
    }

    private static string NormalizeProvider(string provider)
    {
        return string.Equals(provider.Trim(), "CustomHttp", StringComparison.OrdinalIgnoreCase)
            ? "CustomHttp"
            : "Mock";
    }

    private string? UnprotectOptional(string? protectedApiKey)
    {
        return string.IsNullOrWhiteSpace(protectedApiKey) ? null : apiKeyProtector.Unprotect(protectedApiKey);
    }

    private sealed record StoredModelSettings(
        string Provider,
        string BaseUrl,
        string Model,
        string? EncryptedApiKey,
        DateTimeOffset? UpdatedAt)
    {
        public static StoredModelSettings Empty { get; } = new(string.Empty, string.Empty, string.Empty, null, null);
    }

    private sealed record StoredDataSourceSettings(
        bool OfficialAnnouncementsEnabled,
        bool NewsSearchEnabled,
        string MarketDataProvider,
        string MarketDataBaseUrl,
        string? EncryptedMarketDataApiKey,
        string WebResearchProvider,
        string WebResearchBaseUrl,
        string? EncryptedWebResearchApiKey,
        int MaxRequestsPerMinute,
        int RetryCount,
        DateTimeOffset? UpdatedAt)
    {
        public static StoredDataSourceSettings Default { get; } = new(
            true,
            true,
            "Mock",
            string.Empty,
            null,
            "Mock",
            string.Empty,
            null,
            30,
            2,
            null);
    }
}
