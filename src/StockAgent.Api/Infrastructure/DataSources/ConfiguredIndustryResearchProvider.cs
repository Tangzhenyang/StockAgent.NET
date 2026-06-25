using System.Net.Http.Headers;
using System.Text.Json;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>Industry provider backed by the configured datasource gateway. 基于已配置数据源网关的行业数据提供器。</summary>
public sealed class ConfiguredIndustryResearchProvider(
    HttpClient httpClient,
    FakeIndustryResearchProvider fallbackProvider,
    ILogger<ConfiguredIndustryResearchProvider> logger) : IIndustryResearchProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<IndustryResearchSnapshot> GetIndustryAsync(
        string ticker,
        string companyName,
        DataSourceRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(settings.WebResearchProvider, "CustomHttp", StringComparison.OrdinalIgnoreCase))
        {
            return await fallbackProvider.GetIndustryAsync(ticker, companyName, settings, cancellationToken);
        }

        var requestUri = CreateUri(settings.WebResearchBaseUrl, "industry/profile", new Dictionary<string, string>
        {
            ["ticker"] = ticker
        });
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (!string.IsNullOrWhiteSpace(settings.WebResearchApiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.WebResearchApiKey);
        }

        logger.LogInformation("Fetching industry profile for {Ticker} from configured HTTP data source.", ticker);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var snapshot = await response.Content.ReadFromJsonAsync<IndustryResearchSnapshot>(JsonOptions, cancellationToken);
        return snapshot ?? throw new InvalidOperationException("Industry data provider returned an empty response.");
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
}
