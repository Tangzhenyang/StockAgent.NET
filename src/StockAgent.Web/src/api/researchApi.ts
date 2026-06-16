import type { EvidenceCard, PdfExportResponse, ResearchReport, ResearchTask } from '../models';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

/**
 * Creates a new stock research task.
 */
export async function createResearchTask(ticker: string, market: 'AShare' | 'HongKong'): Promise<ResearchTask> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticker, market, language: 'zh-CN' }),
  });

  if (!response.ok) {
    throw new Error(`Create research task failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads the task queue and history.
 */
export async function listResearchTasks(): Promise<ResearchTask[]> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks`);
  if (!response.ok) {
    throw new Error(`List research tasks failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads a generated report for the selected task.
 */
export async function getResearchReport(taskId: string): Promise<ResearchReport> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/report`);
  if (!response.ok) {
    throw new Error(`Get report failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads evidence cards for the selected research task.
 */
export async function listEvidenceCards(taskId: string): Promise<EvidenceCard[]> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/evidence`);
  if (!response.ok) {
    throw new Error(`List evidence cards failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Requests a PDF export for the generated report.
 */
export async function exportResearchReportPdf(taskId: string): Promise<PdfExportResponse> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/pdf`, {
    method: 'POST',
  });

  if (!response.ok) {
    throw new Error(`Export PDF failed with ${response.status}`);
  }

  return response.json();
}
