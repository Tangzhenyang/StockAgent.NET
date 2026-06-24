import type { ResearchReport } from '../models';

/**
 * Renders a generated research report and exposes the PDF export action.
 */
export function ReportViewer({
  report,
  isExporting,
  exportFeedback,
  exportFailed,
  onExportPdf,
}: {
  report?: ResearchReport;
  isExporting: boolean;
  exportFeedback?: string;
  exportFailed?: boolean;
  onExportPdf: () => void;
}) {
  if (!report) {
    return <section className="emptyState">暂无报告</section>;
  }

  return (
    <section className="reportViewer">
      <div className="reportToolbar">
        <h2>研究报告</h2>
        <button type="button" onClick={onExportPdf} disabled={isExporting}>
          {isExporting ? '导出中' : '导出 PDF'}
        </button>
      </div>
      {exportFeedback && <p className={exportFailed ? 'reportFeedback error' : 'reportFeedback'}>{exportFeedback}</p>}
      <article dangerouslySetInnerHTML={{ __html: report.html }} />
    </section>
  );
}
