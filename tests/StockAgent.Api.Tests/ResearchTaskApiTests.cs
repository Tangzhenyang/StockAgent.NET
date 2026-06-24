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
using StockAgent.Api.Infrastructure.Persistence;

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

    /// <summary>
    /// Loading task steps returns ordered, user-owned diagnostic details for the workbench.
    /// 读取任务步骤会为工作台返回按时间排序且归属当前用户的诊断明细。
    /// </summary>
    [Fact]
    public async Task GetResearchTaskSteps_ReturnsOwnedStepsInStartOrder()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "research-steps-user");

        var task = await CreateTaskAsync(client, "700");
        var firstStartedAt = DateTimeOffset.UtcNow.AddMinutes(-2);
        var secondStartedAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
            db.ResearchSteps.AddRange(
                new ResearchStep
                {
                    ResearchTaskId = task.Id,
                    StepName = ResearchStage.CollectStructuredData,
                    Status = StepStatus.Succeeded,
                    StartedAt = firstStartedAt,
                    CompletedAt = firstStartedAt.AddSeconds(3),
                    InputSummary = "请求行情快照",
                    OutputSummary = "行情快照完成"
                },
                new ResearchStep
                {
                    ResearchTaskId = task.Id,
                    StepName = ResearchStage.CollectPublicEvidence,
                    Status = StepStatus.Running,
                    StartedAt = secondStartedAt,
                    InputSummary = "搜索公告与新闻"
                });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/research-tasks/{task.Id}/steps");
        var responseJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseJson);
        using var document = JsonDocument.Parse(responseJson);
        var steps = document.RootElement.EnumerateArray().ToList();
        steps.Should().HaveCountGreaterThanOrEqualTo(2);
        steps[0].GetProperty("stepName").GetString().Should().Be(nameof(ResearchStage.CollectStructuredData));
        steps[1].GetProperty("stepName").GetString().Should().Be(nameof(ResearchStage.CollectPublicEvidence));
        steps[0].GetProperty("durationMs").GetInt64().Should().Be(3000);
        steps[0].GetProperty("outputSummary").GetString().Should().Be("行情快照完成");
        steps[1].GetProperty("status").GetString().Should().Be(nameof(StepStatus.Running));
        steps[1].GetProperty("isLongRunning").GetBoolean().Should().BeFalse();
    }

    private static async Task<ResearchTaskResponse> CreateTaskAsync(HttpClient client, string ticker)
    {
        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest(ticker, Market.HongKong, "zh-CN"));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<ResearchTaskResponse>(json, CreateJsonSerializerOptions())!;
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
