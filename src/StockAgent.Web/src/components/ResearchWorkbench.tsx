import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { FormEvent } from 'react';
import { useMemo, useState } from 'react';
import {
  createResearchTask,
  deleteResearchTask,
  downloadResearchReportPdf,
  exportResearchReportPdf,
  getResearchReport,
  listEvidenceCards,
  listResearchTaskSteps,
  listResearchTasks,
} from '../api/researchApi';
import type { ResearchTask } from '../models';
import { EvidenceDrawer } from './EvidenceDrawer';
import { ReportViewer } from './ReportViewer';
import { TaskStepDetails } from './TaskStepDetails';
import { TaskTimeline } from './TaskTimeline';

const completedStatuses = new Set<ResearchTask['status']>(['Ready', 'Completed']);
const deletableStatuses = new Set<ResearchTask['status']>(['Failed', 'Ready', 'Completed', 'Cancelled']);

/**
 * Main first-screen workbench for submitting stock research tasks and reading reports.
 */
export function ResearchWorkbench() {
  const queryClient = useQueryClient();
  const [ticker, setTicker] = useState('600519');
  const [market, setMarket] = useState<'AShare' | 'HongKong'>('AShare');
  const [selectedTaskId, setSelectedTaskId] = useState<string>();

  const tasksQuery = useQuery({
    queryKey: ['researchTasks'],
    queryFn: () => listResearchTasks(),
    refetchInterval: 3000,
  });
  const tasks = tasksQuery.data ?? [];
  const selectedTask = useMemo(
    () => tasks.find((task) => task.id === selectedTaskId) ?? tasks[0],
    [selectedTaskId, tasks],
  );
  const canLoadReport = selectedTask ? completedStatuses.has(selectedTask.status) : false;

  const reportQuery = useQuery({
    queryKey: ['researchReport', selectedTask?.id],
    queryFn: () => getResearchReport(selectedTask!.id),
    enabled: canLoadReport,
    retry: false,
  });
  const evidenceQuery = useQuery({
    queryKey: ['researchEvidence', selectedTask?.id],
    queryFn: () => listEvidenceCards(selectedTask!.id),
    enabled: Boolean(selectedTask),
    retry: false,
  });
  const stepQuery = useQuery({
    queryKey: ['researchSteps', selectedTask?.id],
    queryFn: () => listResearchTaskSteps(selectedTask!.id),
    enabled: Boolean(selectedTask),
    refetchInterval: 3000,
    retry: false,
  });
  const createMutation = useMutation({
    mutationFn: () => createResearchTask(ticker, market),
    onSuccess: async (task) => {
      setSelectedTaskId(task.id);
      await queryClient.invalidateQueries({ queryKey: ['researchTasks'] });
    },
  });
  const pdfMutation = useMutation({
    mutationFn: async () => {
      const exported = await exportResearchReportPdf(selectedTask!.id);
      await downloadResearchReportPdf(exported.downloadUrl, exported.fileName);
      return exported;
    },
  });
  const deleteMutation = useMutation({
    mutationFn: deleteResearchTask,
    onSuccess: async (_, taskId) => {
      if (selectedTaskId === taskId) {
        setSelectedTaskId(undefined);
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['researchTasks'] }),
        queryClient.removeQueries({ queryKey: ['researchReport', taskId] }),
        queryClient.removeQueries({ queryKey: ['researchEvidence', taskId] }),
        queryClient.removeQueries({ queryKey: ['researchSteps', taskId] }),
      ]);
    },
  });

  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    createMutation.mutate();
  };

  return (
    <main className="workbench">
      <aside className="sidebar">
        <h1>Stock Research Agent</h1>
        <form className="researchForm" onSubmit={handleSubmit}>
          <label>
            股票代码
            <input value={ticker} onChange={(event) => setTicker(event.target.value)} />
          </label>
          <label>
            市场
            <select value={market} onChange={(event) => setMarket(event.target.value as 'AShare' | 'HongKong')}>
              <option value="AShare">A 股</option>
              <option value="HongKong">港股</option>
            </select>
          </label>
          <button type="submit" disabled={createMutation.isPending || ticker.trim().length === 0}>
            {createMutation.isPending ? '提交中' : '开始研究'}
          </button>
        </form>
        <section className="taskList" aria-label="研究任务">
          {tasks.map((task) => {
            const canDelete = deletableStatuses.has(task.status);
            return (
              <div key={task.id} className={task.id === selectedTask?.id ? 'taskItem active' : 'taskItem'}>
                <button type="button" className="taskSelectButton" onClick={() => setSelectedTaskId(task.id)}>
                  <span>{task.ticker}</span>
                  <small>{task.status}</small>
                </button>
                <button
                  type="button"
                  className="taskDeleteButton"
                  disabled={!canDelete || deleteMutation.isPending}
                  title={canDelete ? '删除记录' : '运行中任务不能删除'}
                  onClick={() => {
                    if (window.confirm(`确认删除 ${task.ticker} 的研究记录？`)) {
                      deleteMutation.mutate(task.id);
                    }
                  }}
                >
                  删除
                </button>
              </div>
            );
          })}
        </section>
        {deleteMutation.isError && <p className="formError">删除失败，请确认任务已结束。</p>}
      </aside>
      <section className="content">
        {selectedTask && <TaskTimeline status={selectedTask.status} />}
        {selectedTask && <TaskStepDetails steps={stepQuery.data ?? []} isLoading={stepQuery.isFetching} />}
        <div className="workspaceGrid">
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
          <div className="rightRail">
            <EvidenceDrawer evidenceCards={evidenceQuery.data ?? []} />
          </div>
        </div>
      </section>
    </main>
  );
}
