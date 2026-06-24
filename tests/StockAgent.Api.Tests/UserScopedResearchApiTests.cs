using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies research data is isolated by authenticated user.
/// 验证研究数据按已认证用户隔离。
/// </summary>
public sealed class UserScopedResearchApiTests
{
    /// <summary>
    /// Anonymous callers cannot create research tasks.
    /// 匿名调用方不能创建研究任务。
    /// </summary>
    [Fact]
    public async Task CreateResearchTask_RequiresLogin()
    {
        await using var factory = TestApplicationFactory.Create();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("700", Market.HongKong, "zh-CN"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    /// <summary>
    /// Users cannot list or open another user's research task data.
    /// 用户不能列出或打开其他用户的研究任务数据。
    /// </summary>
    [Fact]
    public async Task ResearchEndpoints_HideOtherUsersData()
    {
        await using var factory = TestApplicationFactory.Create();
        var alice = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var bob = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        await TestApplicationFactory.RegisterAndLoginAsync(alice, "alice-research");
        await TestApplicationFactory.RegisterAndLoginAsync(bob, "bob-research");

        var aliceTask = await CreateTaskAsync(alice, "700");
        var bobTask = await CreateTaskAsync(bob, "600519");
        await SeedCompletedReportAsync(factory.Services, bobTask.Id);

        var aliceList = await alice.GetFromJsonAsync<List<ResearchTaskResponse>>(
            "/api/research-tasks",
            CreateJsonSerializerOptions());
        aliceList.Should().NotBeNull();
        aliceList!.Select(x => x.Id).Should().Contain(aliceTask.Id);
        aliceList.Select(x => x.Id).Should().NotContain(bobTask.Id);

        var aliceCompletedList = await alice.GetFromJsonAsync<List<ResearchTaskResponse>>(
            "/api/research-tasks?status=completed",
            CreateJsonSerializerOptions());
        aliceCompletedList.Should().NotBeNull();
        aliceCompletedList!.Select(x => x.Id).Should().NotContain(bobTask.Id);

        (await alice.GetAsync($"/api/research-tasks/{bobTask.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alice.GetAsync($"/api/research-tasks/{bobTask.Id}/steps")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alice.GetAsync($"/api/research-tasks/{bobTask.Id}/report")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alice.GetAsync($"/api/research-tasks/{bobTask.Id}/evidence")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await alice.PostAsync($"/api/research-tasks/{bobTask.Id}/pdf", null)).StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private static async Task SeedCompletedReportAsync(IServiceProvider serviceProvider, Guid taskId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var task = await db.ResearchTasks.FindAsync(taskId);
        task!.Status = ResearchTaskStatus.Ready;
        task.ProgressPercent = 100;
        db.ResearchReports.Add(new ResearchReport
        {
            ResearchTaskId = taskId,
            Markdown = "# report",
            Html = "<h1>report</h1>",
            RatingJson = "{}"
        });
        db.EvidenceCards.Add(new EvidenceCard
        {
            ResearchTaskId = taskId,
            DocumentSourceId = Guid.NewGuid(),
            DocumentChunkId = Guid.NewGuid(),
            Claim = "claim",
            Snippet = "snippet",
            Confidence = 0.8m,
            Relevance = 0.9m,
            ReportSection = "Business"
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
