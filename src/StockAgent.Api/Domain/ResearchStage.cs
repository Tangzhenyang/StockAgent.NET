namespace StockAgent.Api.Domain;

/// <summary>
/// Ordered pipeline stages used by the research orchestrator.
/// 研究协调器使用的有序流水线阶段。
/// </summary>
public enum ResearchStage
{
    /// <summary>Normalize and validate ticker input. 规范化并验证股票代码输入。</summary>
    NormalizeTicker = 1,
    /// <summary>Collect market profile, price, valuation, and financial snapshots. 收集市场概况、价格、估值和财务快照。</summary>
    CollectStructuredData = 2,
    /// <summary>Collect public documents and web evidence. 收集公开文档和网页证据。</summary>
    CollectPublicEvidence = 3,
    /// <summary>Parse, chunk, and index collected documents. 解析、分块并索引已收集文档。</summary>
    IngestAndIndexDocuments = 4,
    /// <summary>Collect industry profile and recent sector news. 收集行业画像和近期行业新闻。</summary>
    CollectIndustryInformation = 9,
    /// <summary>Run Semantic Kernel-backed analysis over bounded evidence packs. 对受限证据包运行基于 Semantic Kernel 的分析。</summary>
    AnalyzeWithSemanticKernel = 5,
    /// <summary>Create structured scoring and rating output. 创建结构化评分和评级输出。</summary>
    ScoreAndRate = 6,
    /// <summary>Generate the final Chinese Markdown and HTML report. 生成最终的中文 Markdown 和 HTML 报告。</summary>
    GenerateReport = 7,
    /// <summary>Export the report to PDF when requested. 按请求将报告导出为 PDF。</summary>
    ExportPdf = 8
}
