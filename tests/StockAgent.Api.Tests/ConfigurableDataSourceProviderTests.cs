using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies configurable data source providers used by real-data integrations.
/// 验证真实数据集成使用的可配置数据源提供器。
/// </summary>
public sealed class ConfigurableDataSourceProviderTests
{
    /// <summary>
    /// Custom HTTP market data providers expose a stable wrapper contract for AKShare/TuShare services.
    /// 自定义 HTTP 行情数据提供器为 AKShare/TuShare 服务暴露稳定包装契约。
    /// </summary>
    [Fact]
    public async Task ConfiguredMarketDataProvider_UsesCustomHttpSnapshot()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/market/snapshot");
            request.RequestUri.Query.Should().Contain("ticker=00700.HK");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("market-key");
            var json = """
                {
                  "ticker": "00700.HK",
                  "market": "HongKong",
                  "companyName": "腾讯控股",
                  "lastPrice": 320.5,
                  "marketCap": 3000000000000,
                  "peRatio": 18.4,
                  "revenueGrowthPercent": 8.2,
                  "netMarginPercent": 24.5,
                  "quoteSource": "akshare-hk-spot-em",
                  "retrievedAt": "2026-06-25T03:30:00+00:00",
                  "cacheTtlSeconds": 60,
                  "priceFreshness": "intraday-delayed"
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var provider = new ConfiguredMarketDataProvider(
            httpClient,
            new FakeMarketDataProvider(),
            NullLogger<ConfiguredMarketDataProvider>.Instance);
        var settings = CreateRuntimeSettings(marketBaseUrl: "https://provider.example.com/api", marketApiKey: "market-key");

        var snapshot = await provider.GetSnapshotAsync("00700.HK", settings, CancellationToken.None);

        snapshot.CompanyName.Should().Be("腾讯控股");
        snapshot.Market.Should().Be(Market.HongKong);
        snapshot.PeRatio.Should().Be(18.4m);
        snapshot.QuoteSource.Should().Be("akshare-hk-spot-em");
        snapshot.CacheTtlSeconds.Should().Be(60);
        snapshot.PriceFreshness.Should().Be("intraday-delayed");
    }

    /// <summary>
    /// Custom HTTP market data providers accept a service root Base URL and still call the FastAPI /api route.
    /// 自定义 HTTP 行情数据源允许填写服务根地址，并仍然调用 FastAPI 的 /api 路由。
    /// </summary>
    [Fact]
    public async Task ConfiguredMarketDataProvider_AddsApiPrefixWhenBaseUrlIsServiceRoot()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/market/snapshot");
            var json = """
                {
                  "ticker": "00700.HK",
                  "market": "HongKong",
                  "companyName": "腾讯控股",
                  "lastPrice": 320.5,
                  "marketCap": 3000000000000,
                  "peRatio": 18.4,
                  "revenueGrowthPercent": 8.2,
                  "netMarginPercent": 24.5
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var provider = new ConfiguredMarketDataProvider(
            httpClient,
            new FakeMarketDataProvider(),
            NullLogger<ConfiguredMarketDataProvider>.Instance);
        var settings = CreateRuntimeSettings(marketBaseUrl: "http://datasource:8000", marketApiKey: "market-key");

        var snapshot = await provider.GetSnapshotAsync("00700.HK", settings, CancellationToken.None);

        snapshot.CompanyName.Should().Be("腾讯控股");
    }

    /// <summary>
    /// Custom HTTP web research providers expose a stable wrapper contract for announcement/search services.
    /// 自定义 HTTP 网页研究提供器为公告/搜索服务暴露稳定包装契约。
    /// </summary>
    [Fact]
    public async Task ConfiguredWebResearchProvider_UsesCustomHttpDocuments()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/web/search");
            request.RequestUri.Query.Should().Contain("ticker=00700.HK");
            request.RequestUri.Query.Should().Contain("companyName=");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("web-key");
            var json = """
                [
                  {
                    "url": "https://example.com/report.pdf",
                    "title": "腾讯控股 年报",
                    "sourceType": "annual-report",
                    "publishedAt": "2026-03-20T00:00:00+00:00",
                    "text": "收入增长，经营利润率稳定。"
                  }
                ]
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var provider = new ConfiguredWebResearchProvider(
            httpClient,
            new FakeWebResearchProvider(),
            NullLogger<ConfiguredWebResearchProvider>.Instance);
        var settings = CreateRuntimeSettings(webBaseUrl: "https://research.example.com/api", webApiKey: "web-key");

        var documents = await provider.SearchAsync("00700.HK", "腾讯控股", settings, CancellationToken.None);

        documents.Should().ContainSingle();
        documents[0].Title.Should().Be("腾讯控股 年报");
        documents[0].SourceType.Should().Be("annual-report");
    }

    /// <summary>
    /// Custom HTTP web research providers accept a service root Base URL and still call the FastAPI /api route.
    /// 自定义 HTTP 证据数据源允许填写服务根地址，并仍然调用 FastAPI 的 /api 路由。
    /// </summary>
    [Fact]
    public async Task ConfiguredWebResearchProvider_AddsApiPrefixWhenBaseUrlIsServiceRoot()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/web/search");
            var json = """
                [
                  {
                    "url": "https://example.com/report.pdf",
                    "title": "腾讯控股 年报",
                    "sourceType": "annual-report",
                    "publishedAt": "2026-03-20T00:00:00+00:00",
                    "text": "收入增长，经营利润率稳定。"
                  }
                ]
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var provider = new ConfiguredWebResearchProvider(
            httpClient,
            new FakeWebResearchProvider(),
            NullLogger<ConfiguredWebResearchProvider>.Instance);
        var settings = CreateRuntimeSettings(webBaseUrl: "http://datasource:8000", webApiKey: "web-key");

        var documents = await provider.SearchAsync("00700.HK", "腾讯控股", settings, CancellationToken.None);

        documents.Should().ContainSingle();
    }

    /// <summary>
    /// Custom HTTP industry providers reuse the configured evidence datasource gateway.
    /// 自定义 HTTP 行业数据源复用已配置的证据数据源网关。
    /// </summary>
    [Fact]
    public async Task ConfiguredIndustryResearchProvider_UsesCustomHttpIndustryProfile()
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(request =>
        {
            request.RequestUri!.AbsolutePath.Should().Be("/api/industry/profile");
            request.RequestUri.Query.Should().Contain("ticker=301308");
            request.Headers.Authorization!.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be("web-key");
            var json = """
                {
                  "ticker": "301308",
                  "companyName": "江波龙",
                  "industryName": "半导体存储",
                  "sectors": ["半导体", "存储芯片"],
                  "keywords": ["DRAM", "NAND Flash"],
                  "provider": "akshare-news-with-local-industry-map",
                  "retrievedAt": "2026-06-25T03:30:00+00:00",
                  "news": [
                    {
                      "title": "存储价格跟踪",
                      "url": "https://example.com/storage",
                      "source": "example",
                      "publishedAt": "2026-06-25T00:00:00+00:00",
                      "summary": "存储行业消息"
                    }
                  ]
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }));
        var provider = new ConfiguredIndustryResearchProvider(
            httpClient,
            new FakeIndustryResearchProvider(),
            NullLogger<ConfiguredIndustryResearchProvider>.Instance);
        var settings = CreateRuntimeSettings(webBaseUrl: "http://datasource:8000", webApiKey: "web-key");

        var snapshot = await provider.GetIndustryAsync("301308", "江波龙", settings, CancellationToken.None);

        snapshot.IndustryName.Should().Be("半导体存储");
        snapshot.News.Should().ContainSingle();
    }

    private static DataSourceRuntimeSettings CreateRuntimeSettings(
        string marketBaseUrl = "",
        string? marketApiKey = null,
        string webBaseUrl = "",
        string? webApiKey = null)
    {
        return new DataSourceRuntimeSettings(
            true,
            true,
            string.IsNullOrWhiteSpace(marketBaseUrl) ? "Mock" : "CustomHttp",
            marketBaseUrl,
            marketApiKey,
            string.IsNullOrWhiteSpace(webBaseUrl) ? "Mock" : "CustomHttp",
            webBaseUrl,
            webApiKey,
            30,
            2);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
