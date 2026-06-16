/**
 * Durable status values returned by the research task API.
 */
export type ResearchTaskStatus =
  | 'Queued'
  | 'Running'
  | 'CollectingData'
  | 'IngestingDocuments'
  | 'Analyzing'
  | 'GeneratingReport'
  | 'Ready'
  | 'ExportingPdf'
  | 'Completed'
  | 'Failed'
  | 'Cancelled';

/**
 * Research task summary shown in the workbench queue.
 */
export interface ResearchTask {
  id: string;
  ticker: string;
  market: 'AShare' | 'HongKong';
  status: ResearchTaskStatus;
  progressPercent: number;
  language: string;
}

/**
 * Report payload rendered by the report viewer.
 */
export interface ResearchReport {
  markdown: string;
  html: string;
  ratingJson: string;
}

/**
 * Evidence card displayed alongside a generated report.
 */
export interface EvidenceCard {
  id: string;
  claim: string;
  snippet: string;
  confidence: number;
  relevance: number;
  reportSection: string;
}

/**
 * PDF export response returned by the backend.
 */
export interface PdfExportResponse {
  researchTaskId: string;
  filePath: string;
  status: 'Completed';
}
