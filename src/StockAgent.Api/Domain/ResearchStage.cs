namespace StockAgent.Api.Domain;

/// <summary>
/// Ordered pipeline stages used by the research orchestrator.
/// </summary>
public enum ResearchStage
{
    /// <summary>Normalize and validate ticker input.</summary>
    NormalizeTicker = 1,
    /// <summary>Collect market profile, price, valuation, and financial snapshots.</summary>
    CollectStructuredData = 2,
    /// <summary>Collect public documents and web evidence.</summary>
    CollectPublicEvidence = 3,
    /// <summary>Parse, chunk, and index collected documents.</summary>
    IngestAndIndexDocuments = 4,
    /// <summary>Run Semantic Kernel-backed analysis over bounded evidence packs.</summary>
    AnalyzeWithSemanticKernel = 5,
    /// <summary>Create structured scoring and rating output.</summary>
    ScoreAndRate = 6,
    /// <summary>Generate the final Chinese Markdown and HTML report.</summary>
    GenerateReport = 7,
    /// <summary>Export the report to PDF when requested.</summary>
    ExportPdf = 8
}
