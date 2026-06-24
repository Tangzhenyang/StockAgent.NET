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
  createdAt: string;
  updatedAt: string;
}

/**
 * Durable execution status for one research pipeline step.
 */
export type ResearchStepStatus = 'Pending' | 'Running' | 'Succeeded' | 'Failed' | 'Skipped';

/**
 * Detailed diagnostic record for one research pipeline step.
 */
export interface ResearchStep {
  id: string;
  stepName:
    | 'NormalizeTicker'
    | 'CollectStructuredData'
    | 'CollectPublicEvidence'
    | 'IngestAndIndexDocuments'
    | 'AnalyzeWithSemanticKernel'
    | 'ScoreAndRate'
    | 'GenerateReport'
    | 'ExportPdf';
  status: ResearchStepStatus;
  retryCount: number;
  startedAt?: string;
  completedAt?: string;
  durationMs?: number;
  inputSummary?: string;
  outputSummary?: string;
  errorMessage?: string;
  isLongRunning: boolean;
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
  downloadUrl: string;
  fileName: string;
  status: 'Completed';
}

/**
 * Sanitized authenticated user returned by the API.
 */
export interface CurrentUser {
  id: string;
  userName: string;
  isAuthenticated: boolean;
}

/**
 * Sanitized model settings returned by the API.
 */
export interface ModelSettings {
  provider: string;
  baseUrl: string;
  model: string;
  apiKeyConfigured: boolean;
  updatedAt?: string;
}

/**
 * Research settings used to control report generation.
 */
export interface ResearchSettings {
  defaultLanguage: string;
  maxEvidenceCards: number;
  maxDocumentChunks: number;
  maxRetrievedChunks: number;
  retainRawDocuments: boolean;
}

/**
 * Sanitized data source settings returned by the API.
 */
export interface DataSourceSettings {
  officialAnnouncementsEnabled: boolean;
  newsSearchEnabled: boolean;
  marketDataProvider: 'Mock' | 'CustomHttp';
  marketDataBaseUrl: string;
  marketDataApiKeyConfigured: boolean;
  webResearchProvider: 'Mock' | 'CustomHttp';
  webResearchBaseUrl: string;
  webResearchApiKeyConfigured: boolean;
  maxRequestsPerMinute: number;
  retryCount: number;
  updatedAt?: string;
}

/**
 * Combined user settings payload.
 */
export interface UserSettings {
  model: ModelSettings;
  research: ResearchSettings;
  dataSources: DataSourceSettings;
}

/**
 * Model settings update payload sent by the settings form.
 */
export interface SaveModelSettingsRequest {
  provider: string;
  baseUrl: string;
  model: string;
  apiKey?: string;
}

/**
 * Research settings update payload sent by the settings form.
 */
export type SaveResearchSettingsRequest = ResearchSettings;

/**
 * Data source settings update payload sent by the settings form.
 */
export interface SaveDataSourceSettingsRequest {
  officialAnnouncementsEnabled: boolean;
  newsSearchEnabled: boolean;
  marketDataProvider: 'Mock' | 'CustomHttp';
  marketDataBaseUrl: string;
  marketDataApiKey?: string;
  webResearchProvider: 'Mock' | 'CustomHttp';
  webResearchBaseUrl: string;
  webResearchApiKey?: string;
  maxRequestsPerMinute: number;
  retryCount: number;
}

/**
 * Lightweight model connection validation response.
 */
export interface ModelSettingsTestResponse {
  succeeded: boolean;
  message: string;
}

/**
 * Lightweight data source configuration validation response.
 */
export interface DataSourceSettingsTestResponse {
  succeeded: boolean;
  message: string;
}
