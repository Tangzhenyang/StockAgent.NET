import type { ResearchStep } from '../models';

const stageLabels: Record<ResearchStep['stepName'], string> = {
  NormalizeTicker: '规范股票代码',
  CollectStructuredData: '行情/财务采集',
  CollectPublicEvidence: '公告/证据采集',
  IngestAndIndexDocuments: '文档入库',
  AnalyzeWithSemanticKernel: '多 Agent 分析',
  ScoreAndRate: '评分评级',
  GenerateReport: '生成报告',
  ExportPdf: '导出 PDF',
};

const statusLabels: Record<ResearchStep['status'], string> = {
  Pending: '等待',
  Running: '运行中',
  Succeeded: '完成',
  Failed: '失败',
  Skipped: '跳过',
};

/**
 * Shows durable task execution diagnostics for the selected research task.
 */
export function TaskStepDetails({ steps, isLoading }: { steps: ResearchStep[]; isLoading: boolean }) {
  return (
    <section className="stepDetails" aria-label="执行明细">
      <div className="stepDetailsHeader">
        <h2>执行明细</h2>
        <span>{isLoading ? '刷新中' : `${steps.length} 条记录`}</span>
      </div>
      {steps.length === 0 ? (
        <p className="stepEmpty">暂无执行记录</p>
      ) : (
        <ol className="stepList">
          {steps.map((step) => (
            <li key={step.id} className={`stepRow ${step.status.toLowerCase()}`}>
              <div className="stepMain">
                <strong>{stageLabels[step.stepName]}</strong>
                <span>{statusLabels[step.status]}</span>
                <time>{formatTime(step.startedAt)}</time>
                <span>{formatDuration(step.durationMs)}</span>
              </div>
              <div className="stepSummary">
                {step.inputSummary && <p>输入：{step.inputSummary}</p>}
                {step.outputSummary && <p>输出：{step.outputSummary}</p>}
                {step.errorMessage && <p className="stepError">错误：{step.errorMessage}</p>}
                {step.isLongRunning && <p className="stepWarning">运行时间较长，可能是外部数据源或大模型响应较慢。</p>}
              </div>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function formatTime(value?: string) {
  if (!value) {
    return '-';
  }

  return new Intl.DateTimeFormat('zh-CN', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  }).format(new Date(value));
}

function formatDuration(value?: number) {
  if (value === undefined || value === null) {
    return '-';
  }

  if (value < 1000) {
    return `${value} ms`;
  }

  return `${(value / 1000).toFixed(1)} s`;
}
