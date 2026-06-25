using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>
    /// Loading step artifacts returns structured details for a user-owned step.
    /// 读取步骤产物会返回当前用户拥有步骤的结构化详情。
    /// </summary>
    [Fact]
    public async Task GetResearchTaskStepArtifacts_ReturnsOwnedArtifacts()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "research-step-artifacts-user");

        var task = await CreateTaskAsync(client, "700");
        var stepId = Guid.NewGuid();
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
            db.ResearchSteps.Add(new ResearchStep
            {
                Id = stepId,
                ResearchTaskId = task.Id,
                StepName = ResearchStage.CollectStructuredData,
                Status = StepStatus.Succeeded,
                StartedAt = DateTimeOffset.UtcNow.AddSeconds(-1),
                CompletedAt = DateTimeOffset.UtcNow
            });
            db.ResearchStepArtifacts.Add(new ResearchStepArtifact
            {
                ResearchTaskId = task.Id,
                ResearchStepId = stepId,
                Stage = ResearchStage.CollectStructuredData,
                ArtifactType = "market-snapshot",
                Title = "行情/财务快照",
                Summary = "腾讯控股，PE 18.4",
                JsonPayload = """{"companyName":"腾讯控股","peRatio":18.4}"""
            });
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/research-tasks/{task.Id}/steps/{stepId}/artifacts");
        var responseJson = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK, responseJson);
        using var document = JsonDocument.Parse(responseJson);
        var artifacts = document.RootElement.EnumerateArray().ToList();
        artifacts.Should().HaveCount(1);
        artifacts[0].GetProperty("artifactType").GetString().Should().Be("market-snapshot");
        artifacts[0].GetProperty("title").GetString().Should().Be("行情/财务快照");
        artifacts[0].GetProperty("jsonPayload").GetString().Should().Contain("companyName");
    }

    /// <summary>
    /// Deleting a failed task removes its diagnostic and generated child records.
    /// 删除失败任务会清理其诊断记录和生成的子记录。
    /// </summary>
    [Fact]
    public async Task DeleteResearchTask_RemovesFailedTaskWithChildren()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "delete-failed-task-user");

        var task = await SeedTaskAsync(
            factory.Services,
            "delete-failed-task-user",
            "00700.HK",
            Market.HongKong,
            ResearchTaskStatus.Failed,
            DateTimeOffset.UtcNow);
        await SeedTaskChildrenAsync(factory.Services, task.Id, ResearchTaskStatus.Failed);

        var response = await client.DeleteAsync($"/api/research-tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        (await db.ResearchTasks.FindAsync(task.Id)).Should().BeNull();
        db.ResearchSteps.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.ResearchStepArtifacts.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.ResearchReports.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.EvidenceCards.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.DocumentSources.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.DocumentChunks.Should().BeEmpty();
        db.ModelInvocations.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
        db.PdfExports.Where(x => x.ResearchTaskId == task.Id).Should().BeEmpty();
    }

    /// <summary>
    /// Deleting a running task is rejected to avoid racing the background worker.
    /// 删除运行中任务会被拒绝，以避免和后台工作器竞争。
    /// </summary>
    [Fact]
    public async Task DeleteResearchTask_RejectsActiveTask()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "delete-active-task-user");

        var task = await SeedTaskAsync(
            factory.Services,
            "delete-active-task-user",
            "00700.HK",
            Market.HongKong,
            ResearchTaskStatus.CollectingData,
            DateTimeOffset.UtcNow);

        var response = await client.DeleteAsync($"/api/research-tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    /// <summary>
    /// Deleting a stale active task is allowed so old stuck tasks can be cleaned up.
    /// 允许删除过期活动任务，以便清理旧的卡死任务。
    /// </summary>
    [Fact]
    public async Task DeleteResearchTask_AllowsStaleActiveTask()
    {
        await using var factory = TestApplicationFactory.Create();

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(client, "delete-stale-task-user");

        var task = await SeedTaskAsync(
            factory.Services,
            "delete-stale-task-user",
            "00700.HK",
            Market.HongKong,
            ResearchTaskStatus.CollectingData,
            DateTimeOffset.UtcNow.AddMinutes(-30));

        var response = await client.DeleteAsync($"/api/research-tasks/{task.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
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

    private static async Task<ResearchTask> SeedTaskAsync(
        IServiceProvider serviceProvider,
        string userName,
        string ticker,
        Market market,
        ResearchTaskStatus status,
        DateTimeOffset updatedAt)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var user = await db.Users.SingleAsync(x => x.UserName == userName);
        var task = new ResearchTask
        {
            UserId = user.Id,
            Ticker = ticker,
            Market = market,
            Status = status,
            UpdatedAt = updatedAt
        };
        db.ResearchTasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    private static async Task SeedTaskChildrenAsync(
        IServiceProvider serviceProvider,
        Guid taskId,
        ResearchTaskStatus status)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var task = await db.ResearchTasks.FindAsync(taskId);
        task!.Status = status;
        task.UpdatedAt = DateTimeOffset.UtcNow;

        var sourceId = Guid.NewGuid();
        var chunkId = Guid.NewGuid();
        var stepId = Guid.NewGuid();
        db.ResearchSteps.Add(new ResearchStep
        {
            Id = stepId,
            ResearchTaskId = taskId,
            StepName = ResearchStage.CollectStructuredData,
            Status = StepStatus.Failed
        });
        db.ResearchStepArtifacts.Add(new ResearchStepArtifact
        {
            ResearchTaskId = taskId,
            ResearchStepId = stepId,
            Stage = ResearchStage.CollectStructuredData,
            ArtifactType = "market-snapshot",
            Title = "行情快照",
            JsonPayload = "{}"
        });
        db.DocumentSources.Add(new DocumentSource
        {
            Id = sourceId,
            ResearchTaskId = taskId,
            Url = "https://example.com",
            Title = "source",
            SourceType = "news",
            ContentHash = Guid.NewGuid().ToString("N")
        });
        db.DocumentChunks.Add(new DocumentChunk
        {
            Id = chunkId,
            DocumentSourceId = sourceId,
            ChunkIndex = 0,
            Text = "chunk",
            TokenEstimate = 1
        });
        db.EvidenceCards.Add(new EvidenceCard
        {
            ResearchTaskId = taskId,
            DocumentSourceId = sourceId,
            DocumentChunkId = chunkId,
            Claim = "claim",
            Snippet = "snippet",
            Confidence = 0.8m,
            Relevance = 0.9m,
            ReportSection = "Business"
        });
        db.ResearchReports.Add(new ResearchReport
        {
            ResearchTaskId = taskId,
            Markdown = "# report",
            Html = "<h1>report</h1>",
            RatingJson = "{}"
        });
        db.ModelInvocations.Add(new ModelInvocation
        {
            ResearchTaskId = taskId,
            StepName = "agent",
            Provider = "test",
            ModelName = "test",
            DurationMs = 1,
            Status = "Succeeded"
        });
        db.PdfExports.Add(new PdfExport
        {
            ResearchTaskId = taskId,
            Status = "Completed"
        });
        await db.SaveChangesAsync();
    }

    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
