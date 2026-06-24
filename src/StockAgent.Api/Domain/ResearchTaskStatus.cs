namespace StockAgent.Api.Domain;

/// <summary>
/// Durable lifecycle states for a stock research task.
/// 股票研究任务的持久化生命周期状态。
/// </summary>
public enum ResearchTaskStatus
{
    /// <summary>The task has been saved and is waiting for a worker. 任务已保存，等待工作器处理。</summary>
    Queued = 1,
    /// <summary>The worker has started executing the task. 工作器已开始执行任务。</summary>
    Running = 2,
    /// <summary>The task is gathering structured and public source data. 任务正在收集结构化和公开源数据。</summary>
    CollectingData = 3,
    /// <summary>The task is parsing, chunking, and indexing source documents. 任务正在解析、分块并索引源文档。</summary>
    IngestingDocuments = 4,
    /// <summary>The task is running bounded AI-assisted analysis. 任务正在运行受限的 AI 辅助分析。</summary>
    Analyzing = 5,
    /// <summary>The task is converting analysis outputs into the final report. 任务正在将分析结果转换为最终报告。</summary>
    GeneratingReport = 6,
    /// <summary>The report is ready for reading and PDF export. 报告已可阅读并可导出 PDF。</summary>
    Ready = 7,
    /// <summary>The task is exporting a PDF copy of the report. 任务正在导出报告的 PDF 副本。</summary>
    ExportingPdf = 8,
    /// <summary>The research task and any requested PDF export completed successfully. 研究任务及其请求的 PDF 导出已成功完成。</summary>
    Completed = 9,
    /// <summary>The task failed at a specific recoverable stage. 任务在某个可恢复阶段失败。</summary>
    Failed = 10,
    /// <summary>The task was cancelled before completion. 任务在完成前被取消。</summary>
    Cancelled = 11
}
