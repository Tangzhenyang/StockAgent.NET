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
    /// <summary>
    /// Evidence input is capped by card count and snippet length.
    /// 证据输入会按卡片数量和摘要长度裁剪。
    /// </summary>
    [Fact]
    public void BuildEvidenceInput_TruncatesEvidencePackByCountAndSnippetLength()
    {
        var budgeter = new AgentContextBudgeter(new AgentContextBudgetOptions
        {
            MaxEvidenceCardsForEvidenceAgent = 3,
            MaxEvidenceSnippetCharacters = 20
        });
        var cards = Enumerable.Range(0, 10).Select(index => new EvidenceCard
        {
            Id = Guid.NewGuid(),
            Claim = $"证据 {index}",
            Snippet = new string('证', 100),
            Relevance = 1.0m - index * 0.01m,
            Confidence = 0.9m,
            ReportSection = "Financials"
        }).ToList();

        var input = budgeter.BuildEvidenceInput(CreateSnapshot(), cards, "zh-CN");

        input.EvidencePack.Should().HaveCount(3);
        input.EvidencePack.Should().OnlyContain(x => x.Snippet.Length <= 20);
    }

    /// <summary>
    /// Review input contains only bounded report text, key claims, and citations.
    /// 审核输入只包含受限报告文本、关键结论和引用。
    /// </summary>
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
