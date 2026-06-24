# Fixed-Flow Multi-Agent Research Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the current deterministic research analysis stage into a fixed-flow multi-agent pipeline that calls the user's configured LLM through Semantic Kernel and produces evidence-bound Chinese research reports.

**Architecture:** Keep `.NET ResearchOrchestrator` as the durable program-level controller, and implement multiple role-specific LLM agents behind typed interfaces. Run the market/financial agent and evidence/filing agent in parallel, pass their structured JSON outputs to a synthesis report agent, then run a reviewer agent before persisting the final report.

**Tech Stack:** .NET 10, ASP.NET Core BackgroundService, EF Core, PostgreSQL/pgvector, Microsoft Semantic Kernel, OpenAI-compatible chat completion, System.Text.Json, xUnit, FluentAssertions.

---

## File Structure

- Modify: `src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs`
  - Add backend runtime model settings contract.
- Modify: `src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs`
  - Add `GetModelRuntimeSettingsAsync()` that returns decrypted model API settings.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/ResearchAgentContracts.cs`
  - Define inputs/outputs for `MarketFinancialAgent`, `EvidenceFilingAgent`, `SynthesisReportAgent`, and `ReviewAgent`.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/IResearchAgent.cs`
  - Define the generic agent interface.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgetOptions.cs`
  - Define hard per-agent input budgets and list-size limits.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgeter.cs`
  - Build bounded inputs so no LLM agent receives raw oversized documents or unbounded prior outputs.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentOutputValidators.cs`
  - Validate Market/Evidence/Synthesis/Review outputs before passing them to the next agent.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Chat/IModelChatClient.cs`
  - Hide Semantic Kernel chat completion behind a testable JSON-call boundary.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Chat/SemanticKernelModelChatClient.cs`
  - Implement real LLM calls using Semantic Kernel.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/JsonAgentBase.cs`
  - Shared prompt execution, JSON parsing, and one-time JSON repair retry.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/MarketFinancialAgent.cs`
  - Analyze structured market and financial snapshot.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/EvidenceFilingAgent.cs`
  - Analyze selected evidence cards and source citations.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/SynthesisReportAgent.cs`
  - Produce the final structured rating and report draft from previous agent outputs.
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/ReviewAgent.cs`
  - Check whether the report is evidence-bound and not overclaiming.
- Modify: `src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs`
  - Expand the analysis result to carry the final LLM-generated Markdown and agent audit summaries.
- Modify: `src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs`
  - Add model settings and task id to the analysis method.
- Modify: `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`
  - Replace deterministic scoring with the fixed-flow multi-agent pipeline.
- Modify: `src/StockAgent.Api/Infrastructure/Reports/ReportGenerator.cs`
  - Prefer the LLM-generated report Markdown when available, while still producing HTML and score JSON deterministically.
- Modify: `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`
  - Load model settings, pass task id/settings into analysis, and persist model invocation traces.
- Modify: `src/StockAgent.Api/Program.cs`
  - Register chat client and all agent services.
- Test: `tests/StockAgent.Api.Tests/UserSettingsApiTests.cs`
  - Verify runtime model settings decrypt API key for backend use.
- Create: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`
  - Verify agent order, parallel branches, reviewer failure behavior, and JSON output mapping.
- Create: `tests/StockAgent.Api.Tests/AgentContextBudgetTests.cs`
  - Verify EvidenceFilingAgent, SynthesisReportAgent, and ReviewAgent inputs remain bounded.
- Create: `tests/StockAgent.Api.Tests/ReportGeneratorMultiAgentTests.cs`
  - Verify final report uses LLM Markdown and still emits score JSON/HTML.

---

### Task 1: Add Backend Runtime Model Settings

**Files:**
- Modify: `src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs`
- Test: `tests/StockAgent.Api.Tests/UserSettingsApiTests.cs`

- [ ] **Step 1: Write the failing runtime settings test**

Add this test to `tests/StockAgent.Api.Tests/UserSettingsApiTests.cs`:

```csharp
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
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```powershell
dotnet test --filter GetModelRuntimeSettingsAsync_ReturnsDecryptedApiKey
```

Expected: compile failure because `ModelRuntimeSettings` and `GetModelRuntimeSettingsAsync` do not exist.

- [ ] **Step 3: Add runtime model settings contract**

Add this record to `src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs`:

```csharp
/// <summary>
/// Backend-only model settings with decrypted API key. 包含解密 API Key 的后端专用模型配置。
/// </summary>
/// <param name="Provider">Provider kind, such as OpenAICompatible. 提供商类型，例如 OpenAICompatible。</param>
/// <param name="BaseUrl">Model API base URL. 模型 API 基础地址。</param>
/// <param name="Model">Model name. 模型名称。</param>
/// <param name="ApiKey">Decrypted model API key. 解密后的模型 API Key。</param>
public sealed record ModelRuntimeSettings(string Provider, string BaseUrl, string Model, string? ApiKey)
{
    /// <summary>Returns whether the settings are complete enough for a real model call. 返回配置是否足以进行真实模型调用。</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Provider)
        && !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Model)
        && !string.IsNullOrWhiteSpace(ApiKey);
}
```

- [ ] **Step 4: Add backend runtime getter**

Add this method to `src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs`:

```csharp
/// <summary>Gets backend-only model settings with unprotected API key. 获取包含解密密钥的后端专用模型设置。</summary>
public async Task<ModelRuntimeSettings> GetModelRuntimeSettingsAsync(string userId, CancellationToken cancellationToken)
{
    var stored = await LoadModelSettingsAsync(userId, cancellationToken);
    return new ModelRuntimeSettings(
        stored.Provider,
        stored.BaseUrl,
        stored.Model,
        UnprotectOptional(stored.EncryptedApiKey));
}
```

- [ ] **Step 5: Run the test and verify it passes**

Run:

```powershell
dotnet test --filter GetModelRuntimeSettingsAsync_ReturnsDecryptedApiKey
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs tests/StockAgent.Api.Tests/UserSettingsApiTests.cs
git commit -m "feat: expose runtime model settings"
```

---

### Task 2: Define Multi-Agent Contracts

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/IResearchAgent.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/ResearchAgentContracts.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs`
- Test: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`

- [ ] **Step 1: Write contract serialization tests**

Create `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`:

```csharp
using System.Text.Json;
using FluentAssertions;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.Ai.Agents;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the typed contracts used between fixed-flow research agents.
/// 验证固定流程研究 Agent 之间使用的强类型契约。
/// </summary>
public sealed class MultiAgentAnalysisServiceTests
{
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

    private static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions(JsonSerializerDefaults.Web);
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test --filter MultiAgentAnalysisServiceTests
```

Expected: compile failure because agent contracts and new `AiAnalysisResult` fields do not exist.

- [ ] **Step 3: Add generic agent interface**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/IResearchAgent.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Typed boundary for a single role-specific research agent.
/// 单个角色型研究 Agent 的强类型边界。
/// </summary>
/// <typeparam name="TInput">Agent input contract. Agent 输入契约。</typeparam>
/// <typeparam name="TOutput">Agent output contract. Agent 输出契约。</typeparam>
public interface IResearchAgent<in TInput, TOutput>
{
    /// <summary>Agent display name used in logs and model invocation records. 用于日志和模型调用记录的 Agent 名称。</summary>
    string Name { get; }

    /// <summary>Runs the agent for one research subtask. 为一个研究子任务运行 Agent。</summary>
    Task<TOutput> RunAsync(TInput input, CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Add agent input/output contracts**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/ResearchAgentContracts.cs`:

```csharp
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Input for market and financial analysis. 行情与财务分析输入。</summary>
public sealed record MarketFinancialAgentInput(MarketDataSnapshot Snapshot, FinancialTrendSummary? TrendSummary, string Language);

/// <summary>Optional compact financial trend summary. 可选的紧凑财务趋势摘要。</summary>
public sealed record FinancialTrendSummary(
    IReadOnlyList<string> RevenueTrendFacts,
    IReadOnlyList<string> MarginTrendFacts,
    IReadOnlyList<string> PeerComparisonFacts);

/// <summary>Structured output from market and financial analysis. 行情与财务分析结构化输出。</summary>
public sealed record MarketFinancialAgentOutput(
    int Score,
    string ValuationView,
    IReadOnlyList<string> Strengths,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> FollowUpQuestions);

/// <summary>Input for public evidence and filing analysis. 公告与公开证据分析输入。</summary>
public sealed record EvidenceFilingAgentInput(
    MarketDataSnapshot Snapshot,
    IReadOnlyList<EvidencePackItem> EvidencePack,
    string Language);

/// <summary>Bounded citation-ready evidence item passed to LLM agents. 传给 LLM Agent 的受限可引用证据项。</summary>
public sealed record EvidencePackItem(
    Guid EvidenceCardId,
    string Title,
    string Snippet,
    string ReportSection,
    decimal Relevance,
    decimal Confidence,
    DateTimeOffset? SourceDate);

/// <summary>One citation extracted from an evidence card. 从证据卡提取的一条引用。</summary>
public sealed record EvidenceCitation(Guid EvidenceCardId, string Title, string Snippet, DateTimeOffset? SourceDate);

/// <summary>Structured output from public evidence and filing analysis. 公告与公开证据分析结构化输出。</summary>
public sealed record EvidenceFilingAgentOutput(
    IReadOnlyList<string> PositiveFacts,
    IReadOnlyList<string> NegativeFacts,
    IReadOnlyList<string> Uncertainties,
    IReadOnlyList<EvidenceCitation> Citations);

/// <summary>Input for report synthesis. 报告综合输入。</summary>
public sealed record SynthesisReportAgentInput(
    MarketDataSnapshot Snapshot,
    MarketFinancialAgentOutput MarketAnalysis,
    EvidenceFilingAgentOutput EvidenceAnalysis,
    string Language);

/// <summary>Structured final report draft from synthesis. 综合 Agent 生成的结构化报告草稿。</summary>
public sealed record SynthesisReportAgentOutput(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions,
    IReadOnlyList<ReportKeyClaim> KeyClaims,
    string Markdown);

/// <summary>Evidence-bound key claim produced with the final report. 最终报告中带证据绑定的关键结论。</summary>
public sealed record ReportKeyClaim(string Claim, IReadOnlyList<Guid> EvidenceCardIds);

/// <summary>Input for final quality review. 最终质量审核输入。</summary>
public sealed record ReviewAgentInput(
    MarketDataSnapshot Snapshot,
    string ReportMarkdown,
    IReadOnlyList<ReportKeyClaim> KeyClaims,
    IReadOnlyList<EvidenceCitation> Citations,
    string Language);

/// <summary>Review result for evidence binding and overclaim checks. 证据绑定和过度结论检查结果。</summary>
public sealed record ReviewAgentOutput(bool Approved, IReadOnlyList<string> Issues, string RevisionInstruction);
```

- [ ] **Step 5: Expand analysis result**

Modify `src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs` to:

```csharp
namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Structured AI analysis result used by scoring and report generation.
/// 用于评分和报告生成的结构化 AI 分析结果。
/// </summary>
/// <param name="OverallScore">Overall score from 0 to 100. 0 到 100 的综合评分。</param>
/// <param name="RiskLevel">Human-readable risk level. 人类可读的风险等级。</param>
/// <param name="ValuationView">Short valuation conclusion. 简短的估值结论。</param>
/// <param name="Summary">Concise business and investment summary. 简洁的业务与投资摘要。</param>
/// <param name="KeyAssumptions">Assumptions used by the generated analysis. 生成分析所使用的假设。</param>
/// <param name="ReportMarkdown">LLM-generated Markdown report body when available. 可用时由大模型生成的 Markdown 报告正文。</param>
/// <param name="AgentTraces">Human-readable agent execution summaries. 人类可读的 Agent 执行摘要。</param>
public sealed record AiAnalysisResult(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions,
    string? ReportMarkdown = null,
    IReadOnlyList<string>? AgentTraces = null);
```

- [ ] **Step 6: Run tests and verify they pass**

Run:

```powershell
dotnet test --filter MultiAgentAnalysisServiceTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/Agents src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs
git commit -m "feat: define fixed-flow research agent contracts"
```

---

### Task 2A: Add Agent Context Budgets and Output Validators

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgetOptions.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgeter.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentOutputValidators.cs`
- Test: `tests/StockAgent.Api.Tests/AgentContextBudgetTests.cs`

- [ ] **Step 1: Write context budget tests**

Create `tests/StockAgent.Api.Tests/AgentContextBudgetTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies each LLM agent receives bounded context instead of unbounded raw documents or reports.
/// 验证每个 LLM Agent 都接收受限上下文，而不是无界原始文档或报告。
/// </summary>
public sealed class AgentContextBudgetTests
{
    [Fact]
    public void BuildEvidenceInput_TruncatesEvidencePackByCountAndSnippetLength()
    {
        var budgeter = new AgentContextBudgeter(new AgentContextBudgetOptions
        {
            MaxEvidenceCardsForEvidenceAgent = 3,
            MaxEvidenceSnippetCharacters = 20
        });
        var snapshot = CreateSnapshot();
        var cards = Enumerable.Range(0, 10).Select(index => new EvidenceCard
        {
            Id = Guid.NewGuid(),
            Claim = $"证据 {index}",
            Snippet = new string('证', 100),
            Relevance = 1.0m - index * 0.01m,
            Confidence = 0.9m,
            ReportSection = "Financials"
        }).ToList();

        var input = budgeter.BuildEvidenceInput(snapshot, cards, "zh-CN");

        input.EvidencePack.Should().HaveCount(3);
        input.EvidencePack.Should().OnlyContain(x => x.Snippet.Length <= 20);
    }

    [Fact]
    public void BuildReviewInput_UsesReportMarkdownKeyClaimsAndCitationsOnly()
    {
        var budgeter = new AgentContextBudgeter(new AgentContextBudgetOptions
        {
            MaxReviewReportCharacters = 30,
            MaxReviewKeyClaims = 2,
            MaxReviewCitations = 2
        });
        var draft = new SynthesisReportAgentOutput(
            70,
            "中等",
            "估值偏高",
            "摘要",
            ["收入增长延续"],
            [
                new ReportKeyClaim("结论1", [Guid.NewGuid()]),
                new ReportKeyClaim("结论2", [Guid.NewGuid()]),
                new ReportKeyClaim("结论3", [Guid.NewGuid()])
            ],
            new string('报', 200));
        var evidence = new EvidenceFilingAgentOutput(
            [],
            [],
            [],
            [
                new EvidenceCitation(Guid.NewGuid(), "年报1", "摘要1", null),
                new EvidenceCitation(Guid.NewGuid(), "年报2", "摘要2", null),
                new EvidenceCitation(Guid.NewGuid(), "年报3", "摘要3", null)
            ]);

        var input = budgeter.BuildReviewInput(CreateSnapshot(), draft, evidence, "zh-CN");

        input.ReportMarkdown.Length.Should().BeLessThanOrEqualTo(30);
        input.KeyClaims.Should().HaveCount(2);
        input.Citations.Should().HaveCount(2);
    }

    private static MarketDataSnapshot CreateSnapshot()
    {
        return new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1241.41m, 1551863800297m, 57.05m, 6.33m, 52.22m);
    }
}
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test --filter AgentContextBudgetTests
```

Expected: compile failure because `AgentContextBudgeter` and `AgentContextBudgetOptions` do not exist.

- [ ] **Step 3: Add context budget options**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgetOptions.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Hard limits that keep each LLM agent input bounded and predictable.
/// 保持每个 LLM Agent 输入受限且可预测的硬限制。
/// </summary>
public sealed class AgentContextBudgetOptions
{
    /// <summary>Maximum evidence cards passed to EvidenceFilingAgent. 传给 EvidenceFilingAgent 的最大证据卡数量。</summary>
    public int MaxEvidenceCardsForEvidenceAgent { get; init; } = 30;
    /// <summary>Maximum snippet length per evidence item. 每条证据摘要的最大字符数。</summary>
    public int MaxEvidenceSnippetCharacters { get; init; } = 400;
    /// <summary>Maximum strengths emitted by MarketFinancialAgent. MarketFinancialAgent 输出的最大优势数量。</summary>
    public int MaxMarketStrengths { get; init; } = 5;
    /// <summary>Maximum risks emitted by MarketFinancialAgent. MarketFinancialAgent 输出的最大风险数量。</summary>
    public int MaxMarketRisks { get; init; } = 5;
    /// <summary>Maximum positive facts emitted by EvidenceFilingAgent. EvidenceFilingAgent 输出的最大正面事实数量。</summary>
    public int MaxEvidencePositiveFacts { get; init; } = 8;
    /// <summary>Maximum negative facts emitted by EvidenceFilingAgent. EvidenceFilingAgent 输出的最大负面事实数量。</summary>
    public int MaxEvidenceNegativeFacts { get; init; } = 8;
    /// <summary>Maximum citations passed into synthesis and review. 传入综合和审核阶段的最大引用数量。</summary>
    public int MaxCitations { get; init; } = 15;
    /// <summary>Maximum final report Markdown characters reviewed by ReviewAgent. ReviewAgent 审核的最终报告最大字符数。</summary>
    public int MaxReviewReportCharacters { get; init; } = 6000;
    /// <summary>Maximum key claims reviewed by ReviewAgent. ReviewAgent 审核的最大关键结论数量。</summary>
    public int MaxReviewKeyClaims { get; init; } = 12;
    /// <summary>Maximum citations reviewed by ReviewAgent. ReviewAgent 审核的最大引用数量。</summary>
    public int MaxReviewCitations { get; init; } = 15;
}
```

- [ ] **Step 4: Add context budgeter**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgeter.cs`:

```csharp
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Builds bounded agent inputs so raw documents and long prior outputs never flow directly into LLM calls.
/// 构建受限 Agent 输入，避免原始文档和过长中间结果直接进入 LLM 调用。
/// </summary>
public sealed class AgentContextBudgeter(AgentContextBudgetOptions options)
{
    /// <summary>Builds a compact market agent input. 构建紧凑的行情财务 Agent 输入。</summary>
    public MarketFinancialAgentInput BuildMarketInput(MarketDataSnapshot snapshot, string language)
    {
        return new MarketFinancialAgentInput(snapshot, null, language);
    }

    /// <summary>Builds a bounded evidence pack for EvidenceFilingAgent. 为 EvidenceFilingAgent 构建受限证据包。</summary>
    public EvidenceFilingAgentInput BuildEvidenceInput(
        MarketDataSnapshot snapshot,
        IReadOnlyList<EvidenceCard> evidenceCards,
        string language)
    {
        var pack = evidenceCards
            .OrderByDescending(x => x.Relevance)
            .ThenByDescending(x => x.Confidence)
            .Take(options.MaxEvidenceCardsForEvidenceAgent)
            .Select(x => new EvidencePackItem(
                x.Id,
                x.Claim,
                Truncate(x.Snippet, options.MaxEvidenceSnippetCharacters),
                x.ReportSection,
                x.Relevance,
                x.Confidence,
                x.SourceDate))
            .ToList();

        return new EvidenceFilingAgentInput(snapshot, pack, language);
    }

    /// <summary>Builds a bounded synthesis input from validated agent summaries. 从已校验 Agent 摘要构建受限综合输入。</summary>
    public SynthesisReportAgentInput BuildSynthesisInput(
        MarketDataSnapshot snapshot,
        MarketFinancialAgentOutput market,
        EvidenceFilingAgentOutput evidence,
        string language)
    {
        var boundedMarket = market with
        {
            Strengths = market.Strengths.Take(options.MaxMarketStrengths).ToList(),
            Risks = market.Risks.Take(options.MaxMarketRisks).ToList()
        };
        var boundedEvidence = evidence with
        {
            PositiveFacts = evidence.PositiveFacts.Take(options.MaxEvidencePositiveFacts).ToList(),
            NegativeFacts = evidence.NegativeFacts.Take(options.MaxEvidenceNegativeFacts).ToList(),
            Citations = evidence.Citations.Take(options.MaxCitations).ToList()
        };

        return new SynthesisReportAgentInput(snapshot, boundedMarket, boundedEvidence, language);
    }

    /// <summary>Builds a bounded review input from report key claims and citations only. 仅用报告关键结论和引用构建受限审核输入。</summary>
    public ReviewAgentInput BuildReviewInput(
        MarketDataSnapshot snapshot,
        SynthesisReportAgentOutput draft,
        EvidenceFilingAgentOutput evidence,
        string language)
    {
        return new ReviewAgentInput(
            snapshot,
            Truncate(draft.Markdown, options.MaxReviewReportCharacters),
            draft.KeyClaims.Take(options.MaxReviewKeyClaims).ToList(),
            evidence.Citations.Take(options.MaxReviewCitations).ToList(),
            language);
    }

    private static string Truncate(string value, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            return string.Empty;
        }

        return value.Length <= maxCharacters ? value : value[..maxCharacters];
    }
}
```

- [ ] **Step 5: Add output validators**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/AgentOutputValidators.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Deterministic quality gates for agent outputs before the next LLM stage consumes them.
/// Agent 输出进入下一 LLM 阶段前的确定性质量门禁。
/// </summary>
public static class AgentOutputValidators
{
    /// <summary>Validates market analysis output shape and score range. 校验行情财务分析输出结构和分数范围。</summary>
    public static void ValidateMarket(MarketFinancialAgentOutput output)
    {
        if (output.Score is < 0 or > 100)
        {
            throw new InvalidOperationException("MarketFinancialAgent score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(output.ValuationView))
        {
            throw new InvalidOperationException("MarketFinancialAgent valuation view is required.");
        }
    }

    /// <summary>Validates evidence output citations point to real evidence pack ids. 校验证据输出引用来自真实证据包。</summary>
    public static void ValidateEvidence(EvidenceFilingAgentOutput output, EvidenceFilingAgentInput input)
    {
        var allowedIds = input.EvidencePack.Select(x => x.EvidenceCardId).ToHashSet();
        var invalidCitation = output.Citations.FirstOrDefault(x => !allowedIds.Contains(x.EvidenceCardId));
        if (invalidCitation is not null)
        {
            throw new InvalidOperationException($"EvidenceFilingAgent cited unknown evidence card {invalidCitation.EvidenceCardId}.");
        }
    }

    /// <summary>Validates synthesis produced a usable report and evidence-bound key claims. 校验综合阶段生成可用报告和带证据绑定的关键结论。</summary>
    public static void ValidateSynthesis(SynthesisReportAgentOutput output)
    {
        if (output.OverallScore is < 0 or > 100)
        {
            throw new InvalidOperationException("SynthesisReportAgent score must be between 0 and 100.");
        }

        if (string.IsNullOrWhiteSpace(output.Markdown))
        {
            throw new InvalidOperationException("SynthesisReportAgent markdown is required.");
        }

        if (output.KeyClaims.Any(x => x.EvidenceCardIds.Count == 0))
        {
            throw new InvalidOperationException("Each key claim must reference at least one evidence card.");
        }
    }
}
```

- [ ] **Step 6: Run context budget tests**

Run:

```powershell
dotnet test --filter AgentContextBudgetTests
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgetOptions.cs src/StockAgent.Api/Infrastructure/Ai/Agents/AgentContextBudgeter.cs src/StockAgent.Api/Infrastructure/Ai/Agents/AgentOutputValidators.cs tests/StockAgent.Api.Tests/AgentContextBudgetTests.cs
git commit -m "feat: bound multi-agent context"
```

---

### Task 3: Add Testable Semantic Kernel Chat Boundary

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Ai/Chat/IModelChatClient.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Chat/SemanticKernelModelChatClient.cs`
- Test: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`

- [ ] **Step 1: Add fake-client test for JSON completion contract**

Append to `MultiAgentAnalysisServiceTests`:

```csharp
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

private sealed class FakeModelChatClient(string json) : IModelChatClient
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
```

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test --filter FakeModelChatClient_ReturnsConfiguredJson
```

Expected: compile failure because `IModelChatClient` does not exist.

- [ ] **Step 3: Add chat client interface**

Create `src/StockAgent.Api/Infrastructure/Ai/Chat/IModelChatClient.cs`:

```csharp
using StockAgent.Api.Features.UserSettings;

namespace StockAgent.Api.Infrastructure.Ai.Chat;

/// <summary>
/// Testable boundary for model chat completion calls that must return JSON.
/// 必须返回 JSON 的模型聊天补全可测试边界。
/// </summary>
public interface IModelChatClient
{
    /// <summary>Completes one agent prompt and returns raw JSON text. 完成一次 Agent 提示词调用并返回原始 JSON 文本。</summary>
    Task<string> CompleteJsonAsync(
        string agentName,
        string systemPrompt,
        string userPrompt,
        ModelRuntimeSettings settings,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Add Semantic Kernel implementation**

Create `src/StockAgent.Api/Infrastructure/Ai/Chat/SemanticKernelModelChatClient.cs`:

```csharp
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using StockAgent.Api.Features.UserSettings;

namespace StockAgent.Api.Infrastructure.Ai.Chat;

/// <summary>
/// Semantic Kernel implementation for OpenAI-compatible chat completion calls.
/// 面向 OpenAI 兼容聊天补全调用的 Semantic Kernel 实现。
/// </summary>
public sealed class SemanticKernelModelChatClient(ILogger<SemanticKernelModelChatClient> logger) : IModelChatClient
{
    /// <inheritdoc />
    public async Task<string> CompleteJsonAsync(
        string agentName,
        string systemPrompt,
        string userPrompt,
        ModelRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        if (!settings.IsConfigured)
        {
            throw new InvalidOperationException("Model settings are incomplete. Configure provider, base URL, model, and API key.");
        }

        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: settings.Model,
            apiKey: settings.ApiKey!,
            endpoint: new Uri(settings.BaseUrl));
        var kernel = builder.Build();
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(userPrompt);

        logger.LogInformation("Calling model {Model} for {AgentName}.", settings.Model, agentName);
        var response = await chat.GetChatMessageContentAsync(history, kernel: kernel, cancellationToken: cancellationToken);
        return response.Content ?? string.Empty;
    }
}
```

- [ ] **Step 5: Run tests and verify they pass**

Run:

```powershell
dotnet test --filter FakeModelChatClient_ReturnsConfiguredJson
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/Chat tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs
git commit -m "feat: add semantic kernel chat boundary"
```

---

### Task 4: Implement JSON Agent Base and Role Agents

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/JsonAgentBase.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/MarketFinancialAgent.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/EvidenceFilingAgent.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/SynthesisReportAgent.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/Agents/ReviewAgent.cs`
- Test: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`

- [ ] **Step 1: Add JSON parsing test**

Append to `MultiAgentAnalysisServiceTests`:

```csharp
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
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test --filter MarketFinancialAgent_ParsesJsonModelOutput
```

Expected: compile failure because role agents do not exist.

- [ ] **Step 3: Add shared JSON agent base**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/JsonAgentBase.cs`:

```csharp
using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Base class for agents that ask the model for strict JSON and deserialize it into typed output.
/// 请求模型返回严格 JSON 并反序列化为强类型输出的 Agent 基类。
/// </summary>
public abstract class JsonAgentBase<TInput, TOutput>(
    IModelChatClient chatClient,
    ModelRuntimeSettings modelSettings) : IResearchAgent<TInput, TOutput>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public async Task<TOutput> RunAsync(TInput input, CancellationToken cancellationToken)
    {
        var json = await chatClient.CompleteJsonAsync(
            Name,
            BuildSystemPrompt(),
            BuildUserPrompt(input),
            modelSettings,
            cancellationToken);
        return Deserialize(json);
    }

    /// <summary>Builds the system prompt for the role. 构建角色 system prompt。</summary>
    protected abstract string BuildSystemPrompt();

    /// <summary>Builds the user prompt from typed input. 从强类型输入构建 user prompt。</summary>
    protected abstract string BuildUserPrompt(TInput input);

    private static TOutput Deserialize(string json)
    {
        var cleaned = json.Trim();
        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }

        if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3].Trim();
        }

        return JsonSerializer.Deserialize<TOutput>(cleaned, JsonOptions)
               ?? throw new InvalidOperationException($"Model returned empty JSON for {typeof(TOutput).Name}.");
    }
}
```

- [ ] **Step 4: Add model-settings-aware constructors**

Because role agents need per-user settings at runtime, implement constructors that accept both chat client and model settings. Tests can pass explicit settings. Use this pattern in each agent:

```csharp
private static readonly ModelRuntimeSettings TestSettings = new(
    "OpenAICompatible",
    "https://example.test/v1",
    "test-model",
    "test-key");
```

For production construction, Task 5 will use factories that pass the user's runtime settings.

- [ ] **Step 5: Add MarketFinancialAgent**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/MarketFinancialAgent.cs`:

```csharp
using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Analyzes valuation, market data, and financial quality. 分析估值、行情和财务质量。</summary>
public sealed class MarketFinancialAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<MarketFinancialAgentInput, MarketFinancialAgentOutput>(
        chatClient,
        modelSettings ?? new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"))
{
    /// <inheritdoc />
    public override string Name => "MarketFinancialAgent";

    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究中的行情与财务分析 Agent。
        你只分析输入中的结构化行情、估值、市值、收入增长和净利率。
        不得引用未提供的数据，不得给出直接买卖建议。
        必须只输出 JSON，字段为 score, valuationView, strengths, risks, followUpQuestions。
        """;
    }

    protected override string BuildUserPrompt(MarketFinancialAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
```

- [ ] **Step 6: Add EvidenceFilingAgent**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/EvidenceFilingAgent.cs`:

```csharp
using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Analyzes filings and public evidence cards. 分析公告和公开证据卡。</summary>
public sealed class EvidenceFilingAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<EvidenceFilingAgentInput, EvidenceFilingAgentOutput>(
        chatClient,
        modelSettings ?? new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"))
{
    /// <inheritdoc />
    public override string Name => "EvidenceFilingAgent";

    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究中的公告与公开证据分析 Agent。
        你只能基于输入 evidenceCards 提取事实、正面证据、负面证据和不确定性。
        每条关键事实必须能追溯到 evidenceCardId。
        必须只输出 JSON，字段为 positiveFacts, negativeFacts, uncertainties, citations。
        """;
    }

    protected override string BuildUserPrompt(EvidenceFilingAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
```

- [ ] **Step 7: Add SynthesisReportAgent**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/SynthesisReportAgent.cs`:

```csharp
using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Synthesizes agent outputs into a report draft. 将多个 Agent 输出综合为报告草稿。</summary>
public sealed class SynthesisReportAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<SynthesisReportAgentInput, SynthesisReportAgentOutput>(
        chatClient,
        modelSettings ?? new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"))
{
    /// <inheritdoc />
    public override string Name => "SynthesisReportAgent";

    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究报告综合 Agent。
        只能基于 MarketFinancialAgent 和 EvidenceFilingAgent 的输出生成结论。
        不得创造新事实。所有关键结论必须输出到 keyClaims，并绑定 evidenceCardIds。
        报告使用中文 Markdown，不提供直接买卖建议。
        必须只输出 JSON，字段为 overallScore, riskLevel, valuationView, summary, keyAssumptions, keyClaims, markdown。
        """;
    }

    protected override string BuildUserPrompt(SynthesisReportAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
```

- [ ] **Step 8: Add ReviewAgent**

Create `src/StockAgent.Api/Infrastructure/Ai/Agents/ReviewAgent.cs`:

```csharp
using System.Text.Json;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Chat;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>Reviews whether the report is evidence-bound and appropriately cautious. 审核报告是否证据充分且表述审慎。</summary>
public sealed class ReviewAgent(
    IModelChatClient chatClient,
    ModelRuntimeSettings? modelSettings = null)
    : JsonAgentBase<ReviewAgentInput, ReviewAgentOutput>(
        chatClient,
        modelSettings ?? new ModelRuntimeSettings("OpenAICompatible", "https://example.test/v1", "test-model", "test-key"))
{
    /// <inheritdoc />
    public override string Name => "ReviewAgent";

    protected override string BuildSystemPrompt()
    {
        return """
        你是股票研究报告质检 Agent。
        检查报告是否存在没有证据支撑的结论、直接买卖建议、过度确定性表达、引用缺失。
        如果问题不影响报告使用，approved 为 true；如果存在严重问题，approved 为 false。
        必须只输出 JSON，字段为 approved, issues, revisionInstruction。
        """;
    }

    protected override string BuildUserPrompt(ReviewAgentInput input)
    {
        return JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
```

- [ ] **Step 9: Run tests and verify they pass**

Run:

```powershell
dotnet test --filter MultiAgentAnalysisServiceTests
```

Expected: PASS.

- [ ] **Step 10: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/Agents tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs
git commit -m "feat: add fixed-flow research agents"
```

---

### Task 5: Replace Deterministic Analysis with Fixed-Flow Multi-Agent Service

**Files:**
- Modify: `src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`
- Test: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`

- [ ] **Step 1: Add fixed-flow analysis test**

Append to `MultiAgentAnalysisServiceTests`:

```csharp
[Fact]
public async Task SemanticKernelResearchAnalysisService_ReturnsSynthesisWhenReviewerApproves()
{
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
        NullLogger<SemanticKernelResearchAnalysisService>.Instance);
    var snapshot = new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1241.41m, 1551863800297m, 57.05m, 6.33m, 52.22m);
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

private sealed class SequenceModelChatClient(params string[] responses) : IModelChatClient
{
    private readonly Queue<string> _responses = new(responses);

    public Task<string> CompleteJsonAsync(
        string agentName,
        string systemPrompt,
        string userPrompt,
        ModelRuntimeSettings settings,
        CancellationToken cancellationToken)
    {
        return Task.FromResult(_responses.Dequeue());
    }
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test --filter SemanticKernelResearchAnalysisService_ReturnsSynthesisWhenReviewerApproves
```

Expected: compile failure because `AnalyzeAsync` signature and service constructor still use the old deterministic implementation.

- [ ] **Step 3: Update analysis service interface**

Modify `src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs`:

```csharp
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Boundary for Semantic Kernel-backed multi-agent research analysis over bounded evidence packs.
/// 基于受限证据包的 Semantic Kernel 多 Agent 研究分析边界。
/// </summary>
public interface IResearchAnalysisService
{
    /// <summary>Runs the fixed-flow multi-agent analysis. 运行固定流程多 Agent 分析。</summary>
    Task<AiAnalysisResult> AnalyzeAsync(
        Guid researchTaskId,
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        ModelRuntimeSettings modelSettings,
        string language,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 4: Replace analysis service implementation**

Modify `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`:

```csharp
using StockAgent.Api.Domain;
using StockAgent.Api.Features.UserSettings;
using StockAgent.Api.Infrastructure.Ai.Agents;
using StockAgent.Api.Infrastructure.Ai.Chat;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Fixed-flow multi-agent analysis service backed by Semantic Kernel chat completion.
/// 基于 Semantic Kernel 聊天补全的固定流程多 Agent 分析服务。
/// </summary>
public sealed class SemanticKernelResearchAnalysisService(
    IModelChatClient chatClient,
    AgentContextBudgeter contextBudgeter,
    ILogger<SemanticKernelResearchAnalysisService> logger) : IResearchAnalysisService
{
    /// <inheritdoc />
    public async Task<AiAnalysisResult> AnalyzeAsync(
        Guid researchTaskId,
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        ModelRuntimeSettings modelSettings,
        string language,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Starting fixed-flow multi-agent analysis for {Ticker} with {EvidenceCount} evidence cards.",
            marketData.Ticker,
            evidenceCards.Count);

        var marketAgent = new MarketFinancialAgent(chatClient, modelSettings);
        var evidenceAgent = new EvidenceFilingAgent(chatClient, modelSettings);
        var marketInput = contextBudgeter.BuildMarketInput(marketData, language);
        var evidenceInput = contextBudgeter.BuildEvidenceInput(marketData, evidenceCards, language);
        var marketTask = marketAgent.RunAsync(marketInput, cancellationToken);
        var evidenceTask = evidenceAgent.RunAsync(evidenceInput, cancellationToken);
        await Task.WhenAll(marketTask, evidenceTask);
        AgentOutputValidators.ValidateMarket(marketTask.Result);
        AgentOutputValidators.ValidateEvidence(evidenceTask.Result, evidenceInput);

        var synthesisAgent = new SynthesisReportAgent(chatClient, modelSettings);
        var synthesisInput = contextBudgeter.BuildSynthesisInput(marketData, marketTask.Result, evidenceTask.Result, language);
        var synthesis = await synthesisAgent.RunAsync(
            synthesisInput,
            cancellationToken);
        AgentOutputValidators.ValidateSynthesis(synthesis);

        var reviewAgent = new ReviewAgent(chatClient, modelSettings);
        var reviewInput = contextBudgeter.BuildReviewInput(marketData, synthesis, evidenceTask.Result, language);
        var review = await reviewAgent.RunAsync(
            reviewInput,
            cancellationToken);

        if (!review.Approved)
        {
            throw new InvalidOperationException($"Report review failed: {string.Join("; ", review.Issues)}");
        }

        return new AiAnalysisResult(
            synthesis.OverallScore,
            synthesis.RiskLevel,
            synthesis.ValuationView,
            synthesis.Summary,
            synthesis.KeyAssumptions,
            synthesis.Markdown,
            [
                "MarketFinancialAgent:Succeeded",
                "EvidenceFilingAgent:Succeeded",
                "SynthesisReportAgent:Succeeded",
                "ReviewAgent:Approved"
            ]);
    }
}
```

- [ ] **Step 5: Update orchestrator call site**

Modify the analysis section in `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`:

```csharp
await SetStatusAsync(task, ResearchTaskStatus.Analyzing, ResearchStage.AnalyzeWithSemanticKernel, 75, cancellationToken);
var researchSettings = await userSettingsService.GetResearchSettingsAsync(task.UserId, cancellationToken);
var modelSettings = await userSettingsService.GetModelRuntimeSettingsAsync(task.UserId, cancellationToken);
var selectedEvidence = contextBudgetManager.SelectEvidence(evidenceCards, researchSettings.MaxEvidenceCards);
var analysis = await analysisService.AnalyzeAsync(
    task.Id,
    snapshot,
    selectedEvidence,
    modelSettings,
    task.Language,
    cancellationToken);
```

- [ ] **Step 6: Run targeted test**

Run:

```powershell
dotnet test --filter SemanticKernelResearchAnalysisService_ReturnsSynthesisWhenReviewerApproves
```

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs
git commit -m "feat: run fixed-flow multi-agent analysis"
```

---

### Task 6: Persist Model Invocation Audit Records

**Files:**
- Modify: `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs`
- Test: `tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs`

- [ ] **Step 1: Add audit persistence test**

Append to `MultiAgentAnalysisServiceTests`:

```csharp
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
    var snapshot = new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1m, 1m, 1m, 1m, 1m);

    await service.AnalyzeAsync(
        taskId,
        snapshot,
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
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test --filter SemanticKernelResearchAnalysisService_PersistsOneInvocationPerAgent
```

Expected: compile failure because service constructor does not accept `StockAgentDbContext`.

- [ ] **Step 3: Inject DbContext and record successful invocations**

Modify the service constructor:

```csharp
public sealed class SemanticKernelResearchAnalysisService(
    IModelChatClient chatClient,
    AgentContextBudgeter contextBudgeter,
    StockAgentDbContext db,
    ILogger<SemanticKernelResearchAnalysisService> logger) : IResearchAnalysisService
```

Add helper:

```csharp
private async Task<TOutput> RunAndAuditAsync<TInput, TOutput>(
    Guid researchTaskId,
    IResearchAgent<TInput, TOutput> agent,
    TInput input,
    ModelRuntimeSettings modelSettings,
    CancellationToken cancellationToken)
{
    var started = Stopwatch.GetTimestamp();
    try
    {
        var output = await agent.RunAsync(input, cancellationToken);
        db.ModelInvocations.Add(new ModelInvocation
        {
            ResearchTaskId = researchTaskId,
            StepName = agent.Name,
            Provider = modelSettings.Provider,
            ModelName = modelSettings.Model,
            DurationMs = Stopwatch.GetElapsedTime(started).Milliseconds,
            Status = "Succeeded"
        });
        await db.SaveChangesAsync(cancellationToken);
        return output;
    }
    catch (Exception exception)
    {
        db.ModelInvocations.Add(new ModelInvocation
        {
            ResearchTaskId = researchTaskId,
            StepName = agent.Name,
            Provider = modelSettings.Provider,
            ModelName = modelSettings.Model,
            DurationMs = Stopwatch.GetElapsedTime(started).Milliseconds,
            Status = "Failed",
            ErrorMessage = exception.Message
        });
        await db.SaveChangesAsync(cancellationToken);
        throw;
    }
}
```

Use `RunAndAuditAsync()` for all four agents inside `AnalyzeAsync`.

- [ ] **Step 4: Configure ModelInvocation property lengths**

Add this block to `src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs`:

```csharp
modelBuilder.Entity<ModelInvocation>(entity =>
{
    entity.HasKey(x => x.Id);
    entity.Property(x => x.StepName).HasMaxLength(128).IsRequired();
    entity.Property(x => x.Provider).HasMaxLength(128).IsRequired();
    entity.Property(x => x.ModelName).HasMaxLength(256).IsRequired();
    entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
    entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
    entity.HasIndex(x => x.ResearchTaskId);
});
```

- [ ] **Step 5: Run targeted audit test**

Run:

```powershell
dotnet test --filter SemanticKernelResearchAnalysisService_PersistsOneInvocationPerAgent
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs tests/StockAgent.Api.Tests/MultiAgentAnalysisServiceTests.cs
git commit -m "feat: audit multi-agent model invocations"
```

---

### Task 7: Use Agent Markdown in Report Generation

**Files:**
- Modify: `src/StockAgent.Api/Infrastructure/Reports/ReportGenerator.cs`
- Test: `tests/StockAgent.Api.Tests/ReportGeneratorMultiAgentTests.cs`

- [ ] **Step 1: Write report generator test**

Create `tests/StockAgent.Api.Tests/ReportGeneratorMultiAgentTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Reports;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies report generation with LLM-generated multi-agent Markdown.
/// 验证使用多 Agent 大模型 Markdown 生成报告。
/// </summary>
public sealed class ReportGeneratorMultiAgentTests
{
    [Fact]
    public void Generate_UsesMultiAgentMarkdownWhenPresent()
    {
        var generator = new ReportGenerator();
        var snapshot = new MarketDataSnapshot("600519", Market.AShare, "贵州茅台", 1m, 1m, 1m, 1m, 1m);
        var analysis = new AiAnalysisResult(
            68,
            "中等",
            "估值偏高",
            "摘要",
            ["收入增长延续"],
            "# 自定义多 Agent 报告\n\n## 评分结论\n综合评分：68/100");

        var report = generator.Generate(snapshot, analysis, []);

        report.Markdown.Should().StartWith("# 自定义多 Agent 报告");
        report.Html.Should().Contain("自定义多 Agent 报告");
        report.Score.OverallScore.Should().Be(68);
    }
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test --filter Generate_UsesMultiAgentMarkdownWhenPresent
```

Expected: FAIL because `ReportGenerator` ignores `analysis.ReportMarkdown`.

- [ ] **Step 3: Update ReportGenerator**

Modify the Markdown construction in `ReportGenerator.Generate()`:

```csharp
var markdown = string.IsNullOrWhiteSpace(analysis.ReportMarkdown)
    ? $"""
    # {snapshot.CompanyName} {snapshot.Ticker} 深度研究报告

    ## 评分结论

    - 综合评分：{analysis.OverallScore}/100
    - 风险等级：{analysis.RiskLevel}
    - 估值判断：{analysis.ValuationView}

    ## 核心摘要

    {analysis.Summary}

    ## 关键假设

    {string.Join(Environment.NewLine, analysis.KeyAssumptions.Select(x => $"- {x}"))}

    ## 来源证据

    {string.Join(Environment.NewLine, evidenceCards.Select(x => $"- {x.Claim}：{x.Snippet}"))}

    ## 风险提示

    本报告仅用于研究辅助，不构成买卖建议。数据和公开材料可能存在延迟、遗漏或解释偏差。
    """
    : analysis.ReportMarkdown;
```

- [ ] **Step 4: Run report generator test**

Run:

```powershell
dotnet test --filter Generate_UsesMultiAgentMarkdownWhenPresent
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/StockAgent.Api/Infrastructure/Reports/ReportGenerator.cs tests/StockAgent.Api.Tests/ReportGeneratorMultiAgentTests.cs
git commit -m "feat: render multi-agent report markdown"
```

---

### Task 8: Wire Dependency Injection

**Files:**
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs`

- [ ] **Step 1: Add DI smoke test**

Add this test to `tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs`:

```csharp
[Fact]
public void ServiceProvider_ResolvesMultiAgentAnalysisDependencies()
{
    using var factory = TestApplicationFactory.Create();
    using var scope = factory.Services.CreateScope();

    scope.ServiceProvider.GetRequiredService<IModelChatClient>().Should().NotBeNull();
    scope.ServiceProvider.GetRequiredService<AgentContextBudgeter>().Should().NotBeNull();
    scope.ServiceProvider.GetRequiredService<IResearchAnalysisService>().Should().NotBeNull();
}
```

- [ ] **Step 2: Run test and verify it fails**

Run:

```powershell
dotnet test --filter ServiceProvider_ResolvesMultiAgentAnalysisDependencies
```

Expected: compile failure or service resolution failure because `IModelChatClient` is not registered.

- [ ] **Step 3: Register chat client and analysis service**

Modify `src/StockAgent.Api/Program.cs`:

```csharp
builder.Services.AddScoped<IModelChatClient, SemanticKernelModelChatClient>();
builder.Services.AddSingleton(new AgentContextBudgetOptions());
builder.Services.AddScoped<AgentContextBudgeter>();
builder.Services.AddScoped<IResearchAnalysisService, SemanticKernelResearchAnalysisService>();
builder.Services.AddScoped<ReportGenerator>();
```

Remove the old singleton `Kernel.CreateBuilder().Build()` registration if it is no longer used by any service.

- [ ] **Step 4: Run DI smoke test**

Run:

```powershell
dotnet test --filter ServiceProvider_ResolvesMultiAgentAnalysisDependencies
```

Expected: PASS.

- [ ] **Step 5: Commit**

```powershell
git add src/StockAgent.Api/Program.cs tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs
git commit -m "feat: wire multi-agent analysis services"
```

---

### Task 9: End-to-End Pipeline Test with Fake Model Outputs

**Files:**
- Modify: `tests/StockAgent.Api.Tests/TestApplicationFactory.cs`
- Create: `tests/StockAgent.Api.Tests/MultiAgentResearchPipelineTests.cs`

- [ ] **Step 1: Add fake model client override to test factory**

Modify `tests/StockAgent.Api.Tests/TestApplicationFactory.cs` to allow replacing `IModelChatClient`:

```csharp
public static TestApplicationFactory CreateWithModelClient(IModelChatClient modelChatClient)
{
    return Create(services =>
    {
        services.RemoveAll<IModelChatClient>();
        services.AddSingleton(modelChatClient);
    });
}
```

If `Create(Action<IServiceCollection>)` does not exist, add that overload and use it from the existing `Create()` method.

- [ ] **Step 2: Add end-to-end pipeline test**

Create `tests/StockAgent.Api.Tests/MultiAgentResearchPipelineTests.cs`:

```csharp
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
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

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StockAgentDbContext>();
        var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.Markdown.Contains("多 Agent 研究报告"));

        report.Should().NotBeNull();
    }
}
```

- [ ] **Step 3: Run the end-to-end test and verify it fails until background task timing is handled**

Run:

```powershell
dotnet test --filter ResearchPipeline_GeneratesReportWithMultiAgentMarkdown
```

Expected: initial failure if the test checks the report before the background worker completes.

- [ ] **Step 4: Add condition-based wait**

Replace the direct report query with:

```csharp
ResearchReport? report = null;
for (var attempt = 0; attempt < 30; attempt++)
{
    report = await db.ResearchReports.FirstOrDefaultAsync(x => x.Markdown.Contains("多 Agent 研究报告"));
    if (report is not null)
    {
        break;
    }

    await Task.Delay(TimeSpan.FromMilliseconds(200));
}

report.Should().NotBeNull();
```

- [ ] **Step 5: Run the end-to-end test**

Run:

```powershell
dotnet test --filter ResearchPipeline_GeneratesReportWithMultiAgentMarkdown
```

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add tests/StockAgent.Api.Tests/TestApplicationFactory.cs tests/StockAgent.Api.Tests/MultiAgentResearchPipelineTests.cs
git commit -m "test: cover multi-agent research pipeline"
```

---

### Task 10: Validation, Documentation, and Manual Smoke Test

**Files:**
- Modify: `docs/StockAgent.NET_Project_Guide.zh-CN.md`
- Modify: `docs/StockAgent.NET_Project_Guide.zh-CN.pdf`

- [ ] **Step 1: Run all backend tests**

Run:

```powershell
dotnet test
```

Expected: all tests pass, including existing auth/settings/report tests and the new multi-agent tests.

- [ ] **Step 2: Run frontend build**

Run:

```powershell
npm run build
```

Working directory:

```text
C:\Work\WorkSpace\StockAgent.NET\src\StockAgent.Web
```

Expected: TypeScript build and Vite production build pass.

- [ ] **Step 3: Update project guide Markdown**

Add a section to `docs/StockAgent.NET_Project_Guide.zh-CN.md`:

```markdown
## 固定流程多 Agent 研究链路

当前研究链路由 .NET `ResearchOrchestrator` 控制流程，并由多个角色型 Agent 完成分析：

- `MarketFinancialAgent`：分析行情、估值、市值、收入增长、净利率。
- `EvidenceFilingAgent`：分析公告、年报、中报、监管问询和公开证据。
- `SynthesisReportAgent`：综合前两个 Agent 的结构化结果，生成中文 Markdown 报告。
- `ReviewAgent`：检查报告是否存在无证据结论、直接买卖建议或过度确定性表达。

系统采用固定流程而不是自由对话式多 Agent。这样可以保证每个阶段可测试、可追踪、可复现，并便于将每次模型调用记录到 `ModelInvocations`。

上下文管理采用硬预算策略：

- `MarketFinancialAgent` 只接收结构化行情财务摘要，不接收完整财报表格。
- `EvidenceFilingAgent` 只接收 `EvidencePack`，不直接阅读公告全文。
- `SynthesisReportAgent` 只接收前两个 Agent 的结构化摘要和受限引用。
- `ReviewAgent` 只接收报告草稿、关键结论 `keyClaims` 和受限 `citations`。
- 如果输入超过预算，由 `AgentContextBudgeter` 在进入 LLM 前截断、排序和筛选，而不是交给模型自行处理。
```

- [ ] **Step 4: Regenerate Chinese PDF guide**

Use the existing document/PDF generation workflow in the repository. If the current PDF was generated from Markdown by a local script, run that same script. If no script exists, use the previously used PDF toolchain and verify the generated PDF opens.

Expected: `docs/StockAgent.NET_Project_Guide.zh-CN.pdf` includes the new fixed-flow multi-agent section.

- [ ] **Step 5: Manual smoke test with real model configuration**

Use Web UI settings:

```text
模型 Provider: OpenAICompatible
模型 Base URL: 用户真实模型服务地址
模型名称: 用户真实模型名称
模型 API Key: 用户真实 API Key
行情 Base URL: http://127.0.0.1:8014/api
行情 API Key: dev-secret
证据 Base URL: http://127.0.0.1:8014/api
证据 API Key: dev-secret
```

Create a research task:

```text
Ticker: 600519
Market: A股
Language: zh-CN
```

Expected:

```text
任务进入 Ready 状态
报告正文包含多 Agent 生成的 Markdown
ModelInvocations 至少包含 4 条记录
报告不包含直接买卖建议
```

- [ ] **Step 6: Commit documentation**

```powershell
git add docs/StockAgent.NET_Project_Guide.zh-CN.md docs/StockAgent.NET_Project_Guide.zh-CN.pdf
git commit -m "docs: describe fixed-flow multi-agent research"
```

---

## Self-Review

- Spec coverage: The plan covers fixed-flow manager orchestration, market/financial agent, evidence/filing agent, synthesis report agent, reviewer agent, JSON contracts, per-agent context budgets, deterministic output validators, model settings, Semantic Kernel chat boundary, model invocation audit, report rendering, DI, tests, and documentation.
- Placeholder scan: The plan avoids unresolved placeholder markers and unspecified implementation steps. Each code-changing task includes concrete files, snippets, commands, and expected outcomes.
- Type consistency: `ModelRuntimeSettings`, `IModelChatClient`, `IResearchAgent<TInput,TOutput>`, agent input/output records, `AiAnalysisResult.ReportMarkdown`, and the updated `AnalyzeAsync` signature are introduced before dependent tasks use them.
- Scope check: The plan focuses only on fixed-flow multi-agent research analysis and bounded LLM context management. It does not include free-form Semantic Kernel `AgentGroupChat`, vector retrieval changes, UI redesign, or new data source providers.
