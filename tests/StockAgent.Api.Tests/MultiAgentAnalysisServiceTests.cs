using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies fixed-flow research agent contracts and orchestration behavior.
/// 验证固定流程研究 Agent 契约和编排行为。
/// </summary>
public sealed class MultiAgentAnalysisServiceTests
{
    /// <summary>
    /// Market agent output contract can round-trip through JSON.
    /// 行情财务 Agent 输出契约可以通过 JSON 往返。
    /// </summary>
    [Fact]
    public void MarketFinancialAgentOutput_RoundTripsThroughJson()
    {
        var output = new MarketFinancialAgentOutput(
            72,
            "估值偏高但盈利质量稳定",
            ["净利率稳定"],
            ["PE 高于保守区间"],
            ["需要持续跟踪收入增长"]);

        var json = JsonSerializer.Serialize(output, JsonOptions());
        var parsed = JsonSerializer.Deserialize<MarketFinancialAgentOutput>(json, JsonOptions());

        parsed.Should().BeEquivalentTo(output);
    }

    /// <summary>
    /// Analysis result can carry generated Markdown and agent traces.
    /// 分析结果可以携带生成的 Markdown 和 Agent 轨迹。
    /// </summary>
    [Fact]
    public void AiAnalysisResult_CanCarryGeneratedMarkdown()
    {
        var result = new AiAnalysisResult(
            73,
            "中等",
            "估值需要结合增长验证",
            "摘要",
            ["收入增长延续"],
            "## LLM 报告正文",
            ["MarketFinancialAgent:Succeeded"]);

        result.ReportMarkdown.Should().Contain("LLM 报告正文");
        result.AgentTraces.Should().Contain("MarketFinancialAgent:Succeeded");
    }

    /// <summary>
    /// Fake chat clients can be used by tests without calling real providers.
    /// 测试可以使用假聊天客户端而不调用真实提供商。
    /// </summary>
    [Fact]
    public async Task FakeModelChatClient_ReturnsConfiguredJson()
    {
        var client = new FakeModelChatClient("""{"score":81}""");

        var json = await client.CompleteJsonAsync(
            "UnitTestAgent",
            "system",
            "user",
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            CancellationToken.None);

        json.Should().Be("""{"score":81}""");
    }

    /// <summary>
    /// MarketFinancialAgent parses strict JSON model output into its typed contract.
    /// MarketFinancialAgent 会把严格 JSON 模型输出解析为强类型契约。
    /// </summary>
    [Fact]
    public async Task MarketFinancialAgent_ParsesJsonModelOutput()
    {
        var client = new FakeModelChatClient("""
        {
          "score": 76,
          "valuationView": "估值需要结合增长验证",
          "strengths": ["净利率稳定"],
          "risks": ["PE 偏高"],
          "followUpQuestions": ["收入增长能否延续"]
        }
        """);
        var agent = new MarketFinancialAgent(client);
        var snapshot = new MarketDataSnapshot(
            "600519",
            Market.AShare,
            "贵州茅台",
            1241.41m,
            1551863800297m,
            57.05m,
            6.33m,
            52.22m);

        var output = await agent.RunAsync(
            new MarketFinancialAgentInput(snapshot, null, "zh-CN"),
            CancellationToken.None);

        output.Score.Should().Be(76);
        output.Risks.Should().Contain("PE 偏高");
    }

    /// <summary>
    /// MarketFinancialAgent tolerates model scores returned as strings with units.
    /// MarketFinancialAgent 可以容忍模型把评分返回为带单位的字符串。
    /// </summary>
    [Fact]
    public async Task MarketFinancialAgent_ParsesStringScoreModelOutput()
    {
        var client = new FakeModelChatClient("""
        {
          "score": "70分",
          "valuationView": "估值偏高",
          "strengths": ["净利率稳定"],
          "risks": ["PE 偏高"],
          "followUpQuestions": []
        }
        """);
        var agent = new MarketFinancialAgent(client);

        var output = await agent.RunAsync(
            new MarketFinancialAgentInput(CreateSnapshot(), null, "zh-CN"),
            CancellationToken.None);

        output.Score.Should().Be(70);
    }

    /// <summary>
    /// Fixed-flow analysis returns the synthesis result when review approves.
    /// 审核通过时固定流程分析会返回综合结果。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_ReturnsSynthesisWhenReviewerApproves()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """
            {"score":70,"valuationView":"估值偏高","strengths":["净利率高"],"risks":["PE 偏高"],"followUpQuestions":["增长能否延续"]}
            """,
            """
            {"positiveFacts":["年度报告已披露"],"negativeFacts":["需求存在波动"],"uncertainties":["宏观需求"],"citations":[{"evidenceCardId":"11111111-1111-1111-1111-111111111111","title":"年报","snippet":"收入增长","sourceDate":null}]}
            """,
            """
            {"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"盈利质量稳定但估值需要验证","keyAssumptions":["收入增长延续"],"keyClaims":[{"claim":"盈利质量稳定但估值需要验证","evidenceCardIds":["11111111-1111-1111-1111-111111111111"]}],"markdown":"# 贵州茅台 深度研究报告\n\n## 评分结论\n综合评分：68/100"}
            """,
            """
            {"approved":true,"issues":[],"revisionInstruction":""}
            """);
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);
        var snapshot = CreateSnapshot();
        var evidence = new[]
        {
            new EvidenceCard
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Claim = "年报披露收入增长",
                Snippet = "收入增长",
                Relevance = 0.92m,
                Confidence = 0.82m,
                ReportSection = "Financials"
            }
        };

        var result = await service.AnalyzeAsync(
            Guid.NewGuid(),
            snapshot,
            evidence,
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None);

        result.OverallScore.Should().Be(68);
        result.ReportMarkdown.Should().Contain("贵州茅台 深度研究报告");
        result.AgentTraces.Should().Contain("ReviewAgent:Approved");
    }

    /// <summary>
    /// Evidence agent output with loose string citations is tolerated and sanitized before synthesis.
    /// 证据 Agent 返回宽松字符串引用时会被容忍，并在综合前清洗。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_SanitizesLooseEvidenceCitations()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """
            {"score":70,"valuationView":"估值偏高","strengths":["净利率高"],"risks":["PE 偏高"],"followUpQuestions":[]}
            """,
            """
            {"positiveFacts":["年度报告已披露"],"negativeFacts":[],"uncertainties":[],"citations":["年报显示收入增长"]}
            """,
            """
            {"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"摘要","keyAssumptions":[],"keyClaims":[],"markdown":"# 报告"}
            """,
            """
            {"approved":true,"issues":[],"revisionInstruction":""}
            """);
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);

        var result = await service.AnalyzeAsync(
            Guid.NewGuid(),
            CreateSnapshot(),
            [],
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None);

        result.ReportMarkdown.Should().Contain("# 报告");
    }

    /// <summary>
    /// Synthesis key claims without evidence card ids are removed instead of failing the workflow.
    /// 综合 Agent 返回无证据绑定关键结论时会被移除，而不是让工作流失败。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_DropsUnboundSynthesisKeyClaims()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """
            {"score":70,"valuationView":"估值偏高","strengths":["净利率高"],"risks":["PE 偏高"],"followUpQuestions":[]}
            """,
            """
            {"positiveFacts":["年度报告已披露"],"negativeFacts":[],"uncertainties":[],"citations":[{"evidenceCardId":"11111111-1111-1111-1111-111111111111","title":"年报","snippet":"收入增长","sourceDate":null}]}
            """,
            """
            {"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"摘要","keyAssumptions":[],"keyClaims":[{"claim":"盈利质量稳定","evidenceCardIds":[]}],"markdown":"# 报告"}
            """,
            """
            {"approved":true,"issues":[],"revisionInstruction":""}
            """);
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);
        var evidence = new[]
        {
            new EvidenceCard
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Claim = "年报披露收入增长",
                Snippet = "收入增长",
                Relevance = 0.92m,
                Confidence = 0.82m,
                ReportSection = "Financials"
            }
        };

        var result = await service.AnalyzeAsync(
            Guid.NewGuid(),
            CreateSnapshot(),
            evidence,
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None);

        result.ReportMarkdown.Should().Contain("# 报告");
    }

    /// <summary>
    /// Review failures are converted into a limited report instead of failing analysis.
    /// 审核失败会转换为受限报告，而不是让分析失败。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_ReturnsLimitedReportWhenReviewFails()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """
            {"score":70,"valuationView":"估值偏高","strengths":["净利率高"],"risks":["PE 偏高"],"followUpQuestions":[]}
            """,
            """
            {"positiveFacts":["年度报告已披露"],"negativeFacts":[],"uncertainties":["毛利率持续性需要验证"],"citations":[]}
            """,
            """
            {"overallScore":62,"riskLevel":"较高","valuationView":"需要更多证据验证","summary":"摘要","keyAssumptions":["未来三年增长延续"],"keyClaims":[],"markdown":"# 研报草稿\n\n## 核心结论\n增长延续。"}
            """,
            """
            {"approved":false,"issues":["关键假设缺少证据支撑","部分结论无法从现有公告推断"],"revisionInstruction":"补充证据不足说明"}
            """);
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);

        var result = await service.AnalyzeAsync(
            Guid.NewGuid(),
            CreateSnapshot(),
            [],
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None);

        result.ReportMarkdown.Should().Contain("证据不足与无法推断事项");
        result.ReportMarkdown.Should().Contain("关键假设缺少证据支撑");
        result.AgentTraces.Should().Contain("ReviewAgent:LimitedReport");
    }

    /// <summary>
    /// Fixed-flow analysis writes one model invocation record per agent.
    /// 固定流程分析会为每个 Agent 写入一条模型调用记录。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_PersistsOneInvocationPerAgent()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """{"score":70,"valuationView":"估值偏高","strengths":[],"risks":[],"followUpQuestions":[]}""",
            """{"positiveFacts":[],"negativeFacts":[],"uncertainties":[],"citations":[]}""",
            """{"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"摘要","keyAssumptions":[],"keyClaims":[],"markdown":"# 报告"}""",
            """{"approved":true,"issues":[],"revisionInstruction":""}""");
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);
        var taskId = Guid.NewGuid();

        await service.AnalyzeAsync(
            taskId,
            CreateSnapshot(),
            [],
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None);

        var invocations = await db.ModelInvocations.Where(x => x.ResearchTaskId == taskId).ToListAsync();
        invocations.Should().HaveCount(4);
        invocations.Select(x => x.StepName).Should().Contain([
            "MarketFinancialAgent",
            "EvidenceFilingAgent",
            "SynthesisReportAgent",
            "ReviewAgent"
        ]);
        invocations.Should().OnlyContain(x => x.PromptTokens > 0 && x.CompletionTokens > 0);
    }

    /// <summary>
    /// Industry data triggers the industry research agent and carries its trace into synthesis.
    /// 行业数据会触发行业研究 Agent，并把轨迹带入综合阶段。
    /// </summary>
    [Fact]
    public async Task SemanticKernelResearchAnalysisService_RunsIndustryAgentWhenIndustryDataExists()
    {
        await using var factory = TestApplicationFactory.Create();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var client = new SequenceModelChatClient(
            """{"score":70,"valuationView":"估值偏高","strengths":[],"risks":[],"followUpQuestions":[]}""",
            """{"positiveFacts":[],"negativeFacts":[],"uncertainties":[],"citations":[]}""",
            """{"industryView":"存储行业景气度待验证","opportunities":["价格修复"],"risks":["周期波动"],"newsHighlights":["存储新闻"],"followUpQuestions":["价格是否延续"]}""",
            """{"overallScore":68,"riskLevel":"中等","valuationView":"估值偏高","summary":"摘要","keyAssumptions":[],"keyClaims":[],"markdown":"# 报告\n\n## 行业景气度\n存储行业待验证。"}""",
            """{"approved":true,"issues":[],"revisionInstruction":""}""");
        var service = new SemanticKernelResearchAnalysisService(
            client,
            new AgentContextBudgeter(new AgentContextBudgetOptions()),
            db,
            NullLogger<SemanticKernelResearchAnalysisService>.Instance);

        var result = await service.AnalyzeAsync(
            Guid.NewGuid(),
            CreateSnapshot(),
            [],
            new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"),
            "zh-CN",
            CancellationToken.None,
            new IndustryResearchSnapshot(
                "301308",
                "江波龙",
                "半导体存储",
                ["半导体", "存储芯片"],
                ["DRAM", "NAND Flash"],
                "test",
                DateTimeOffset.UtcNow,
                [new IndustryNewsItem("存储新闻", "https://example.com", "test", DateTimeOffset.UtcNow, "存储行业消息")]));

        result.AgentTraces.Should().Contain("IndustryResearchAgent:Succeeded");
        result.ReportMarkdown.Should().Contain("行业景气度");
    }

    internal sealed class FakeModelChatClient(string json) : IModelChatClient
    {
        public Task<string> CompleteJsonAsync(
            string agentName,
            string systemPrompt,
            string userPrompt,
            ModelRuntimeSettings settings,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(json);
        }
    }

    internal sealed class SequenceModelChatClient(params string[] responses) : IModelChatClient
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

    private static MarketDataSnapshot CreateSnapshot()
    {
        return new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1241.41m, 1551863800297m, 57.05m, 6.33m, 52.22m);
    }

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
