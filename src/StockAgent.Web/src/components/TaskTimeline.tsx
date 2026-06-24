import type { ResearchTaskStatus } from '../models';

const stages: ResearchTaskStatus[] = [
  'Queued',
  'CollectingData',
  'IngestingDocuments',
  'Analyzing',
  'GeneratingReport',
  'Ready',
  'ExportingPdf',
  'Completed',
];

const labels: Record<ResearchTaskStatus, string> = {
  Queued: '排队',
  Running: '运行',
  CollectingData: '采集',
  IngestingDocuments: '入库',
  Analyzing: '分析',
  GeneratingReport: '报告',
  Ready: '就绪',
  ExportingPdf: '导出',
  Completed: '完成',
  Failed: '失败',
  Cancelled: '取消',
};

/**
 * Displays first-version research progress as a compact horizontal timeline.
 */
export function TaskTimeline({ status }: { status: ResearchTaskStatus }) {
  const activeIndex = status === 'Failed' ? 0 : Math.max(0, stages.indexOf(status));

  return (
    <ol className="timeline" aria-label="研究进度">
      {stages.map((stage, index) => {
        const state = getStepState(status, index, activeIndex);
        return (
          <li key={stage} className={`timelineItem ${state}`} aria-current={state === 'current' ? 'step' : undefined}>
            <span className="timelineMarker" aria-hidden="true" />
            <span className="timelineLabel">{labels[stage]}</span>
          </li>
        );
      })}
    </ol>
  );
}

function getStepState(status: ResearchTaskStatus, index: number, activeIndex: number) {
  if (status === 'Failed' && index === activeIndex) {
    return 'failed';
  }

  if (status === 'Completed' || index < activeIndex) {
    return 'done';
  }

  return index === activeIndex ? 'current' : 'pending';
}
