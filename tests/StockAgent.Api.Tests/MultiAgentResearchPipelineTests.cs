using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the complete research pipeline can generate a report through fixed-flow agents.
/// 验证完整研究流水线可以通过固定流程 Agent 生成报告。
/// </summary>
public sealed class MultiAgentResearchPipelineTests
{
    /// <summary>
    /// A queued research task can complete with model-generated Markdown.
    /// 入队研究任务可以用模型生成的 Markdown 完成报告。
    /// </summary>
    [Fact]
    public async Task ResearchPipeline_GeneratesReportWithMultiAgentMarkdown()
    {
        await using var factory = TestApplicationFactory.CreateWithModelClient(new SequenceModelChatClient(
            """{"score":70,"valuationView":"估值偏高","strengths":["净利率高"],"risks":["PE 偏高"],"followUpQuestions":[]}""",
            """{"positiveFacts":["年报已披露"],"negativeFacts":[],"uncertainties":[],"citations":[]}""",
            """{"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"摘要","keyAssumptions":["增长延续"],"keyClaims":[],"markdown":"# 多 Agent 研究报告"}""",
            """{"approved":true,"issues":[],"revisionInstruction":""}"""));

        var client = factory.CreateClient();
        await TestApplicationFactory.RegisterAndLoginAsync(client, "multi-agent-pipeline-user");

        var settingsResponse = await client.PutAsJsonAsync(
            "/api/user-settings/model",
            new SaveModelSettingsRequest("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"));
        settingsResponse.EnsureSuccessStatusCode();

        var createResponse = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("600519", Market.AShare, "zh-CN"));
        createResponse.EnsureSuccessStatusCode();

        ResearchReport? report = null;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
            report = await db.ResearchReports.FirstOrDefaultAsync(x => x.Markdown.Contains("多 Agent 研究报告"));
            if (report is not null)
            {
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(200));
        }

        report.Should().NotBeNull();
    }

    private sealed class SequenceModelChatClient(params string[] responses) : IModelChatClient
    {
        private readonly Queue<string> _responses = new(responses);
        private readonly object _lock = new();

        public Task<string> CompleteJsonAsync(
            string agentName,
            string systemPrompt,
            string userPrompt,
            ModelRuntimeSettings settings,
            CancellationToken cancellationToken)
        {
            lock (_lock)
            {
                return Task.FromResult(_responses.Dequeue());
            }
        }
    }
}
