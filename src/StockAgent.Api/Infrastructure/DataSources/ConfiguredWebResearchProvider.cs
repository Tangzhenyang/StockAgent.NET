using System.Net.Http.Headers;
using System.Text.Json;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Web research provider that routes to mock evidence or a user-configured HTTP wrapper service.
/// 根据用户配置路由到 Mock 证据或自定义 HTTP 包装服务的网页研究提供器。
/// </summary>
public sealed class ConfiguredWebResearchProvider(
    HttpClient httpClient,
    FakeWebResearchProvider fallbackProvider,
    ILogger<ConfiguredWebResearchProvider> logger) : IWebResearchProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(
        string ticker,
        string companyName,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        if (!IsCustomHttp(settings.WebResearchProvider))
        {
            return await fallbackProvider.SearchAsync(ticker, companyName, settings, cancellationToken);
        }

        var requestUri = CreateUri(settings.WebResearchBaseUrl, "web/search", new Dictionary<string, string>
        {
            ["ticker"] = ticker,
            ["companyName"] = companyName
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        AddBearerToken(request, settings.WebResearchApiKey);

        logger.LogInformation("Fetching web research documents for {Ticker} from configured HTTP data source.", ticker);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var documents = await response.Content.ReadFromJsonAsync<List<WebEvidenceDocument>>(JsonOptions, cancellationToken);
        return documents ?? [];
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
        var builder = new UriBuilder($"{baseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}");
        builder.Query = string.Join("&", query.Select(x =>
            $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value)}"));
        return builder.Uri;
    }
}
