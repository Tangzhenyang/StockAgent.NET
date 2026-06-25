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
        await EnsureSuccessWithProviderBodyAsync(response, cancellationToken);

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

    private static async Task EnsureSuccessWithProviderBodyAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var detail = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase : SummarizeProviderError(body);
        throw new HttpRequestException(
            $"Market data provider returned {(int)response.StatusCode} ({response.ReasonPhrase}). {detail}",
            null,
            response.StatusCode);
    }

    private static string SummarizeProviderError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            var provider = ReadJsonString(root, "provider");
            var error = ReadJsonString(root, "error") ?? ReadJsonString(root, "detail");
            if (!string.IsNullOrWhiteSpace(provider) || !string.IsNullOrWhiteSpace(error))
            {
                return $"Provider={provider ?? "unknown"}; Error={error ?? "unknown"}";
            }
        }
        catch (JsonException)
        {
            // Non-JSON upstream errors are still useful, so fall through to the raw text summary.
        }

        return body.Length > 1000 ? body[..1000] : body;
    }

    private static string? ReadJsonString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
