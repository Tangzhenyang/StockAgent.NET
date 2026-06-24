using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Settings;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies per-user model and research settings APIs.
/// 验证按用户隔离的模型和研究设置 API。
/// </summary>
public sealed class UserSettingsApiTests
{
    /// <summary>
    /// Anonymous users cannot read settings.
    /// 匿名用户不能读取设置。
    /// </summary>
    [Fact]
    public async Task GetUserSettings_RequiresLogin()
    {
        await using var factory = TestApplicationFactory.Create();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/user-settings");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Authenticated users can save settings without receiving API key secrets back.
    /// 已登录用户可以保存设置，且不会收到 API Key 密文或明文。
    /// </summary>
    [Fact]
    public async Task SaveUserSettings_ReturnsSanitizedConfiguration()
    {
        await using var factory = TestApplicationFactory.Create();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "settings-user");

        var modelResponse = await client.PutAsJsonAsync(
            "/api/user-settings/model",
            new
            {
                provider = "OpenAICompatible",
                baseUrl = "https://api.example.com/v1",
                model = "deep-research-test",
                apiKey = "secret-key-123"
            });
        var modelJson = await modelResponse.Content.ReadAsStringAsync();

        modelResponse.StatusCode.Should().Be(HttpStatusCode.OK, modelJson);
        modelJson.Should().NotContain("secret-key-123");
        modelJson.Should().NotContain("encryptedApiKey");
        using (var document = JsonDocument.Parse(modelJson))
        {
            document.RootElement.GetProperty("provider").GetString().Should().Be("OpenAICompatible");
            document.RootElement.GetProperty("apiKeyConfigured").GetBoolean().Should().BeTrue();
        }

        var researchResponse = await client.PutAsJsonAsync(
            "/api/user-settings/research",
            new
            {
                defaultLanguage = "zh-CN",
                maxEvidenceCards = 12,
                maxDocumentChunks = 200,
                maxRetrievedChunks = 20,
                retainRawDocuments = false
            });
        researchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var settingsResponse = await client.GetAsync("/api/user-settings");
        var settingsJson = await settingsResponse.Content.ReadAsStringAsync();

        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK, settingsJson);
        settingsJson.Should().NotContain("secret-key-123");
        using var settingsDocument = JsonDocument.Parse(settingsJson);
        settingsDocument.RootElement.GetProperty("model").GetProperty("apiKeyConfigured").GetBoolean().Should().BeTrue();
        settingsDocument.RootElement.GetProperty("research").GetProperty("maxEvidenceCards").GetInt32().Should().Be(12);
    }

    /// <summary>
    /// Authenticated users can save data source settings without receiving source API keys back.
    /// 已登录用户可以保存数据源设置，且不会收到数据源 API Key 明文或密文。
    /// </summary>
    [Fact]
    public async Task SaveDataSourceSettings_ReturnsSanitizedConfiguration()
    {
        await using var factory = TestApplicationFactory.Create();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "data-source-user");

        var saveResponse = await client.PutAsJsonAsync(
            "/api/user-settings/data-sources",
            new
            {
                officialAnnouncementsEnabled = true,
                newsSearchEnabled = true,
                marketDataProvider = "CustomHttp",
                marketDataBaseUrl = "https://market.example.com/api",
                marketDataApiKey = "market-secret-123",
                webResearchProvider = "CustomHttp",
                webResearchBaseUrl = "https://research.example.com/api",
                webResearchApiKey = "web-secret-456",
                maxRequestsPerMinute = 30,
                retryCount = 2
            });
        var saveJson = await saveResponse.Content.ReadAsStringAsync();

        saveResponse.StatusCode.Should().Be(HttpStatusCode.OK, saveJson);
        saveJson.Should().NotContain("market-secret-123");
        saveJson.Should().NotContain("web-secret-456");
        saveJson.Should().NotContain("encrypted");
        using (var saveDocument = JsonDocument.Parse(saveJson))
        {
            saveDocument.RootElement.GetProperty("marketDataProvider").GetString().Should().Be("CustomHttp");
            saveDocument.RootElement.GetProperty("marketDataApiKeyConfigured").GetBoolean().Should().BeTrue();
            saveDocument.RootElement.GetProperty("webResearchProvider").GetString().Should().Be("CustomHttp");
            saveDocument.RootElement.GetProperty("webResearchApiKeyConfigured").GetBoolean().Should().BeTrue();
        }

        var settingsResponse = await client.GetAsync("/api/user-settings");
        var settingsJson = await settingsResponse.Content.ReadAsStringAsync();

        settingsResponse.StatusCode.Should().Be(HttpStatusCode.OK, settingsJson);
        settingsJson.Should().NotContain("market-secret-123");
        settingsJson.Should().NotContain("web-secret-456");
        using var settingsDocument = JsonDocument.Parse(settingsJson);
        var dataSources = settingsDocument.RootElement.GetProperty("dataSources");
        dataSources.GetProperty("officialAnnouncementsEnabled").GetBoolean().Should().BeTrue();
        dataSources.GetProperty("marketDataBaseUrl").GetString().Should().Be("https://market.example.com/api");
        dataSources.GetProperty("maxRequestsPerMinute").GetInt32().Should().Be(30);
    }

    /// <summary>
    /// Backend runtime model settings return the decrypted API key for model calls.
    /// 后端运行时模型配置会返回解密后的 API Key 供模型调用使用。
    /// </summary>
    [Fact]
    public async Task GetModelRuntimeSettingsAsync_ReturnsDecryptedApiKey()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var settingsService = scope.ServiceProvider.GetRequiredService<UserSettingsService>();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var user = new ApplicationUser { UserName = "runtime-model-user", Email = "runtime-model-user@example.com" };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        await settingsService.SaveModelSettingsAsync(
            user.Id,
            new SaveModelSettingsRequest(
                "OpenAICompatible",
                "https://api.deepseek.com/v1",
                "deepseek-chat",
                "sk-test-model-key"),
            CancellationToken.None);

        var runtime = await settingsService.GetModelRuntimeSettingsAsync(user.Id, CancellationToken.None);

        runtime.Provider.Should().Be("OpenAICompatible");
        runtime.BaseUrl.Should().Be("https://api.deepseek.com/v1");
        runtime.Model.Should().Be("deepseek-chat");
        runtime.ApiKey.Should().Be("sk-test-model-key");
    }
}
