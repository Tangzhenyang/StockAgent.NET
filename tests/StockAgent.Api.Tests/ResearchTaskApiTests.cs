using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the research task API contract used by the React workbench.
/// 验证 React 工作台使用的研究任务 API 契约。
/// </summary>
public sealed class ResearchTaskApiTests
{
    /// <summary>
    /// Creating a Hong Kong research task returns a string-enum response with normalized ticker.
    /// 创建港股研究任务后会返回带有规范化股票代码的字符串枚举响应。
    /// </summary>
    [Fact]
    public async Task CreateResearchTask_ReturnsCreatedTask()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "research-task-user");

        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("700", Market.HongKong, "zh-CN"));
        var responseJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Created, responseJson);
        responseJson.Should().Contain("\"market\":\"HongKong\"");
        responseJson.Should().Contain("\"status\":\"Queued\"");

        var body = JsonSerializer.Deserialize<ResearchTaskResponse>(responseJson, CreateJsonSerializerOptions());
        body.Should().NotBeNull();
        body!.Ticker.Should().Be("00700.HK");
        body.Status.Should().Be(ResearchTaskStatus.Queued);
    }

    /// <summary>
    /// The API host can resolve fixed-flow multi-agent analysis dependencies.
    /// API 主机可以解析固定流程多 Agent 分析依赖。
    /// </summary>
    [Fact]
    public void ServiceProvider_ResolvesMultiAgentAnalysisDependencies()
    {
        using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();

        scope.ServiceProvider.GetRequiredService<IModelChatClient>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<AgentContextBudgeter>().Should().NotBeNull();
        scope.ServiceProvider.GetRequiredService<IResearchAnalysisService>().Should().NotBeNull();
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
