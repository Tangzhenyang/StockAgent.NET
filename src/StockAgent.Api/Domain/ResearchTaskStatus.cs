namespace StockAgent.Api.Domain;

/// <summary>
/// Durable lifecycle states for a stock research task.
/// </summary>
public enum ResearchTaskStatus
{
    /// <summary>The task has been saved and is waiting for a worker.</summary>
    Queued = 1,
    /// <summary>The worker has started executing the task.</summary>
    Running = 2,
    /// <summary>The task is gathering structured and public source data.</summary>
    CollectingData = 3,
    /// <summary>The task is parsing, chunking, and indexing source documents.</summary>
    IngestingDocuments = 4,
    /// <summary>The task is running bounded AI-assisted analysis.</summary>
    Analyzing = 5,
    /// <summary>The task is converting analysis outputs into the final report.</summary>
    GeneratingReport = 6,
    /// <summary>The report is ready for reading and PDF export.</summary>
    Ready = 7,
    /// <summary>The task is exporting a PDF copy of the report.</summary>
    ExportingPdf = 8,
    /// <summary>The research task and any requested PDF export completed successfully.</summary>
    Completed = 9,
    /// <summary>The task failed at a specific recoverable stage.</summary>
    Failed = 10,
    /// <summary>The task was cancelled before completion.</summary>
    Cancelled = 11
}
