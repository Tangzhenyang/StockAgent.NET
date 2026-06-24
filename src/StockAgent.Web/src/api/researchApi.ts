import type { EvidenceCard, PdfExportResponse, ResearchReport, ResearchStep, ResearchTask } from '../models';

export const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

/**
 * Creates a new stock research task.
 */
export async function createResearchTask(ticker: string, market: 'AShare' | 'HongKong'): Promise<ResearchTask> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    credentials: 'include',
    body: JSON.stringify({ ticker, market }),
  });

  if (!response.ok) {
    throw new Error(`Create research task failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads the task queue and history.
 */
export async function listResearchTasks(status?: 'completed'): Promise<ResearchTask[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : '';
  const response = await fetch(`${apiBaseUrl}/api/research-tasks${query}`, {
    credentials: 'include',
  });
  if (!response.ok) {
    throw new Error(`List research tasks failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads a generated report for the selected task.
 */
export async function getResearchReport(taskId: string): Promise<ResearchReport> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/report`, {
    credentials: 'include',
  });
  if (!response.ok) {
    throw new Error(`Get report failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads evidence cards for the selected research task.
 */
export async function listEvidenceCards(taskId: string): Promise<EvidenceCard[]> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/evidence`, {
    credentials: 'include',
  });
  if (!response.ok) {
    throw new Error(`List evidence cards failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads detailed execution steps for the selected research task.
 */
export async function listResearchTaskSteps(taskId: string): Promise<ResearchStep[]> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/steps`, {
    credentials: 'include',
  });
  if (!response.ok) {
    throw new Error(`List research task steps failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Requests a PDF export for the generated report.
 */
export async function exportResearchReportPdf(taskId: string): Promise<PdfExportResponse> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/pdf`, {
    method: 'POST',
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Export PDF failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Downloads a generated PDF through the authenticated browser session.
 */
export async function downloadResearchReportPdf(downloadUrl: string, fileName: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}${downloadUrl}`, {
    credentials: 'include',
  });

  if (!response.ok) {
    throw new Error(`Download PDF failed with ${response.status}`);
  }

  const blob = await response.blob();
  const objectUrl = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = objectUrl;
  anchor.download = fileName;
  document.body.appendChild(anchor);
  anchor.click();
  anchor.remove();
  URL.revokeObjectURL(objectUrl);
}
