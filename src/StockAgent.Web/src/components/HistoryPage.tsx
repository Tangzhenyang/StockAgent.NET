import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import {
  downloadResearchReportPdf,
  exportResearchReportPdf,
  getResearchReport,
  listEvidenceCards,
  listResearchTasks,
} from '../api/researchApi';
import type { ResearchTask } from '../models';
import { EvidenceDrawer } from './EvidenceDrawer';
import { ReportViewer } from './ReportViewer';

/**
 * Lists completed research tasks and renders the selected historical report.
 */
export function HistoryPage() {
  const queryClient = useQueryClient();
  const [selectedTaskId, setSelectedTaskId] = useState<string>();
  const tasksQuery = useQuery({
    queryKey: ['researchTasks', 'completed'],
    queryFn: () => listResearchTasks('completed'),
    refetchInterval: 5000,
  });
  const tasks = tasksQuery.data ?? [];
  const selectedTask = useMemo<ResearchTask | undefined>(
    () => tasks.find((task) => task.id === selectedTaskId) ?? tasks[0],
    [selectedTaskId, tasks],
  );
  const reportQuery = useQuery({
    queryKey: ['researchReport', selectedTask?.id],
    queryFn: () => getResearchReport(selectedTask!.id),
    enabled: Boolean(selectedTask),
    retry: false,
  });
  const evidenceQuery = useQuery({
    queryKey: ['researchEvidence', selectedTask?.id],
    queryFn: () => listEvidenceCards(selectedTask!.id),
    enabled: Boolean(selectedTask),
    retry: false,
  });
  const pdfMutation = useMutation({
    mutationFn: async () => {
      const exported = await exportResearchReportPdf(selectedTask!.id);
      await downloadResearchReportPdf(exported.downloadUrl, exported.fileName);
      return exported;
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['researchTasks', 'completed'] });
    },
  });

  return (
    <main className="historyPage">
      <aside className="historyList" aria-label="历史报告">
        <h1>历史记录</h1>
        {tasks.length === 0 ? (
          <p className="muted">暂无已完成报告</p>
        ) : (
          <ul>
            {tasks.map((task) => (
              <li key={task.id}>
                <button
                  type="button"
                  className={task.id === selectedTask?.id ? 'historyItem active' : 'historyItem'}
                  onClick={() => setSelectedTaskId(task.id)}
                >
                  <span>{task.ticker}</span>
                  <small>{formatDate(task.updatedAt)}</small>
                </button>
              </li>
            ))}
          </ul>
        )}
      </aside>
      <section className="historyContent">
        <ReportViewer
          report={reportQuery.data}
          isExporting={pdfMutation.isPending}
          exportFeedback={
            pdfMutation.isError
              ? 'PDF 导出失败，请确认后端已安装 Playwright Chromium。'
              : pdfMutation.isSuccess
                ? 'PDF 已开始下载。'
                : undefined
          }
          exportFailed={pdfMutation.isError}
          onExportPdf={() => pdfMutation.mutate()}
        />
        <EvidenceDrawer evidenceCards={evidenceQuery.data ?? []} />
      </section>
    </main>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('zh-CN', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value));
}
