using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Market data provider that routes to mock data or a user-configured HTTP wrapper service.
/// 根据用户配置路由到 Mock 数据或自定义 HTTP 包装服务的行情数据提供器。
/// </summary>
public sealed class ConfiguredMarketDataProvider(
    HttpClient httpClient,
    FakeMarketDataProvider fallbackProvider,
    ILogger<ConfiguredMarketDataProvider> logger) : IMarketDataProvider
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    /// <inheritdoc />
    public async Task<MarketDataSnapshot> GetSnapshotAsync(
        string ticker,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        if (!IsCustomHttp(settings.MarketDataProvider))
        {
            return await fallbackProvider.GetSnapshotAsync(ticker, settings, cancellationToken);
        }

        var requestUri = CreateUri(settings.MarketDataBaseUrl, "market/snapshot", new Dictionary<string, string>
        {
            ["ticker"] = ticker
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        AddBearerToken(request, settings.MarketDataApiKey);

        logger.LogInformation("Fetching market snapshot for {Ticker} from configured HTTP data source.", ticker);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var snapshot = await response.Content.ReadFromJsonAsync<MarketDataSnapshot>(JsonOptions, cancellationToken);
        return snapshot ?? throw new InvalidOperationException("Market data provider returned an empty response.");
    }

    private static bool IsCustomHttp(string provider)
    {
        return string.Equals(provider, "CustomHttp", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddBearerToken(HttpRequestMessage request, string? apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private static Uri CreateUri(string baseUrl, string relativePath, IReadOnlyDictionary<string, string> query)
    {
        var normalizedBaseUrl = baseUrl.TrimEnd('/');
        var normalizedRelativePath = relativePath.TrimStart('/');
        if (!normalizedBaseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            normalizedRelativePath = $"api/{normalizedRelativePath}";
        }

        var builder = new UriBuilder($"{normalizedBaseUrl}/{normalizedRelativePath}");
        builder.Query = string.Join("&", query.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return builder.Uri;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
