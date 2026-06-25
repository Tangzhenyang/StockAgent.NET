import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { listResearchStepArtifacts } from '../api/researchApi';
import type { ResearchStep, ResearchStepArtifact } from '../models';

const stageLabels: Record<ResearchStep['stepName'], string> = {
  NormalizeTicker: '规范股票代码',
  CollectStructuredData: '行情/财务采集',
  CollectPublicEvidence: '公告/证据采集',
  IngestAndIndexDocuments: '文档入库',
  CollectIndustryInformation: '行业信息采集',
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

const marketSnapshotLabels: Record<string, string> = {
  ticker: '股票代码',
  market: '市场',
  companyName: '公司名称',
  lastPrice: '最新价',
  marketCap: '总市值',
  peRatio: '市盈率 PE',
  revenueGrowthPercent: '营收增长率',
  netMarginPercent: '净利率',
  quoteSource: '行情来源',
  retrievedAt: '获取时间',
  cacheTtlSeconds: '缓存秒数',
  priceFreshness: '价格口径',
};

const genericLabels: Record<string, string> = {
  overallScore: '综合评分',
  riskLevel: '风险等级',
  valuationView: '估值观点',
  summary: '摘要',
  tokenUsageNote: 'Token 说明',
};

const invocationLabels: Record<string, string> = {
  stepName: 'Agent',
  provider: '模型供应商',
  modelName: '模型',
  promptTokens: '输入 Token',
  completionTokens: '输出 Token',
  totalTokens: '总 Token',
  durationMs: '耗时',
  status: '状态',
  errorMessage: '错误',
  createdAt: '调用时间',
};

/**
 * Shows durable task execution diagnostics for the selected research task.
 */
export function TaskStepDetails({ taskId, steps, isLoading }: { taskId: string; steps: ResearchStep[]; isLoading: boolean }) {
  const [expandedStepId, setExpandedStepId] = useState<string>();
  const artifactQuery = useQuery({
    queryKey: ['researchStepArtifacts', taskId, expandedStepId],
    queryFn: () => listResearchStepArtifacts(taskId, expandedStepId!),
    enabled: Boolean(expandedStepId),
    retry: false,
  });

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
          {steps.map((step) => {
            const expanded = expandedStepId === step.id;
            return (
              <li key={step.id} className={`stepRow ${step.status.toLowerCase()}`}>
                <button
                  type="button"
                  className="stepToggle"
                  aria-expanded={expanded}
                  onClick={() => setExpandedStepId(expanded ? undefined : step.id)}
                >
                  <div className="stepMain">
                    <strong>{stageLabels[step.stepName]}</strong>
                    <span>{statusLabels[step.status]}</span>
                    <time>{formatTime(step.startedAt)}</time>
                    <span>{formatDuration(step.durationMs)}</span>
                  </div>
                  <span className="stepToggleHint">{expanded ? '收起' : '查看详情'}</span>
                </button>
                <div className="stepSummary">
                  {step.inputSummary && <p>输入：{step.inputSummary}</p>}
                  {step.outputSummary && <p>输出：{step.outputSummary}</p>}
                  {step.errorMessage && <p className="stepError">错误：{step.errorMessage}</p>}
                  {step.isLongRunning && <p className="stepWarning">运行时间较长，可能是外部数据源或大模型响应较慢。</p>}
                </div>
                {expanded && (
                  <StepArtifacts
                    artifacts={artifactQuery.data ?? []}
                    isLoading={artifactQuery.isFetching}
                    isError={artifactQuery.isError}
                  />
                )}
              </li>
            );
          })}
        </ol>
      )}
    </section>
  );
}

function StepArtifacts({
  artifacts,
  isLoading,
  isError,
}: {
  artifacts: ResearchStepArtifact[];
  isLoading: boolean;
  isError: boolean;
}) {
  if (isLoading) {
    return <p className="stepArtifactEmpty">正在加载阶段详情...</p>;
  }

  if (isError) {
    return <p className="stepArtifactEmpty error">阶段详情加载失败。</p>;
  }

  if (artifacts.length === 0) {
    return <p className="stepArtifactEmpty">暂无阶段详情，旧任务可能没有记录结构化产物。</p>;
  }

  return (
    <div className="stepArtifacts">
      {artifacts.map((artifact) => (
        <article key={artifact.id} className="stepArtifact">
          <header>
            <strong>{artifact.title}</strong>
            <span>{artifact.summary}</span>
          </header>
          <ArtifactPayload artifact={artifact} />
        </article>
      ))}
    </div>
  );
}

function ArtifactPayload({ artifact }: { artifact: ResearchStepArtifact }) {
  const payload = parsePayload(artifact.jsonPayload);
  if (artifact.artifactType === 'market-snapshot' && isRecord(payload)) {
    return <KeyValueGrid value={payload} labels={marketSnapshotLabels} valueFormatter={formatMarketValue} />;
  }

  if (artifact.artifactType === 'source-documents' && Array.isArray(payload)) {
    return (
      <ul className="artifactList">
        {payload.map((source, index) => {
          const item = toRecord(source);
          return (
            <li key={index}>
              <strong>{String(item.title ?? item.Title ?? '未命名来源')}</strong>
              {renderUrl(item.url ?? item.Url)}
              <span>{String(item.sourceType ?? item.SourceType ?? '未知来源类型')}</span>
              <span>{formatOptionalDate(item.publishedAt ?? item.PublishedAt)}</span>
            </li>
          );
        })}
      </ul>
    );
  }

  if (artifact.artifactType === 'ingested-evidence' && Array.isArray(payload)) {
    return (
      <ul className="artifactList">
        {payload.map((source, index) => {
          const item = toRecord(source);
          return (
            <li key={index}>
              <strong>{String(item.title ?? item.Title ?? '源文档')}</strong>
              {renderUrl(item.url ?? item.Url)}
              <span>Chunk：{String(item.chunkCount ?? item.ChunkCount ?? 0)}</span>
              <NestedEvidenceList cards={toArray(item.evidenceCards ?? item.EvidenceCards)} />
            </li>
          );
        })}
      </ul>
    );
  }

  if (artifact.artifactType === 'agent-analysis' && isRecord(payload)) {
    return (
      <div className="artifactStack">
        <KeyValueGrid
          labels={genericLabels}
          value={{
            overallScore: payload.overallScore,
            riskLevel: payload.riskLevel,
            valuationView: payload.valuationView,
            summary: payload.summary,
            tokenUsageNote: payload.tokenUsageNote,
          }}
        />
        <ModelInvocationList invocations={toArray(payload.modelInvocations ?? payload.ModelInvocations)} />
      </div>
    );
  }

  if (artifact.artifactType === 'industry-profile' && isRecord(payload)) {
    return (
      <div className="artifactStack">
        <KeyValueGrid
          labels={{
            ticker: '股票代码',
            companyName: '公司名称',
            industryName: '行业名称',
            sectors: '相关赛道',
            keywords: '关键词',
            provider: '数据来源',
            retrievedAt: '获取时间',
          }}
          value={{
            ticker: payload.ticker,
            companyName: payload.companyName,
            industryName: payload.industryName,
            sectors: payload.sectors,
            keywords: payload.keywords,
            provider: payload.provider,
            retrievedAt: payload.retrievedAt,
          }}
          valueFormatter={formatIndustryValue}
        />
        <NestedEvidenceList cards={toArray(payload.news)} title="行业消息" />
      </div>
    );
  }

  return <pre className="artifactJson">{artifact.jsonPayload}</pre>;
}

function KeyValueGrid({
  value,
  labels = {},
  valueFormatter = formatValue,
}: {
  value: Record<string, unknown>;
  labels?: Record<string, string>;
  valueFormatter?: (key: string, value: unknown) => string;
}) {
  return (
    <dl className="artifactGrid">
      {Object.entries(value).map(([key, entry]) => (
        <div key={key}>
          <dt>{labels[key] ?? key}</dt>
          <dd>{valueFormatter(key, entry)}</dd>
        </div>
      ))}
    </dl>
  );
}

function NestedEvidenceList({ cards, title = '证据卡' }: { cards: unknown[]; title?: string }) {
  if (!Array.isArray(cards) || cards.length === 0) {
    return null;
  }

  return (
    <div className="nestedArtifactList">
      <span>{title}</span>
      <ul>
        {cards.map((card, index) => (
          <li key={index}>{formatValue(card)}</li>
        ))}
      </ul>
    </div>
  );
}

function ModelInvocationList({ invocations }: { invocations: unknown[] }) {
  if (invocations.length === 0) {
    return null;
  }

  return (
    <div className="modelInvocationList">
      <span>模型调用</span>
      {invocations.map((invocation, index) => (
        <KeyValueGrid
          key={index}
          labels={invocationLabels}
          value={toRecord(invocation)}
          valueFormatter={formatInvocationValue}
        />
      ))}
    </div>
  );
}

function parsePayload(value: string): unknown {
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}

function toRecord(value: unknown): Record<string, unknown> {
  return isRecord(value) ? value : {};
}

function toArray(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

function renderUrl(value: unknown) {
  if (typeof value !== 'string' || value.length === 0) {
    return null;
  }

  return (
    <a href={value} target="_blank" rel="noreferrer">
      {value}
    </a>
  );
}

function formatOptionalDate(value: unknown) {
  return typeof value === 'string' && value.length > 0 ? new Date(value).toLocaleString('zh-CN') : '发布时间未知';
}

function formatValue(value: unknown): string {
  if (value === null || value === undefined) {
    return '-';
  }

  if (typeof value === 'object') {
    return JSON.stringify(value);
  }

  return String(value);
}

function formatMarketValue(key: string, value: unknown): string {
  if (key === 'market') {
    if (value === 1 || value === '1' || value === 'AShare') {
      return 'A 股';
    }

    if (value === 2 || value === '2' || value === 'HongKong') {
      return '港股';
    }
  }

  if (key === 'marketCap' && typeof value === 'number') {
    return `${(value / 100000000).toFixed(2)} 亿元`;
  }

  if (['revenueGrowthPercent', 'netMarginPercent'].includes(key) && typeof value === 'number') {
    return `${value.toFixed(2)}%`;
  }

  if (key === 'peRatio' && typeof value === 'number') {
    return value.toFixed(2);
  }

  if (key === 'lastPrice' && typeof value === 'number') {
    return value.toFixed(2);
  }

  if (key === 'retrievedAt' && typeof value === 'string') {
    return new Date(value).toLocaleString('zh-CN');
  }

  if (key === 'priceFreshness') {
    if (value === 'intraday-delayed') {
      return '盘中延迟行情';
    }

    if (value === 'daily-close-fallback') {
      return '日线收盘价兜底';
    }
  }

  if (key === 'cacheTtlSeconds' && typeof value === 'number') {
    return `${value} 秒`;
  }

  return formatValue(value);
}

function formatInvocationValue(key: string, value: unknown): string {
  if (key === 'durationMs' && typeof value === 'number') {
    return formatDuration(value);
  }

  if (key === 'createdAt' && typeof value === 'string') {
    return new Date(value).toLocaleString('zh-CN');
  }

  if (['promptTokens', 'completionTokens', 'totalTokens'].includes(key)) {
    return value === null || value === undefined || value === 0 ? '未返回/未估算' : String(value);
  }

  return formatValue(value);
}

function formatIndustryValue(key: string, value: unknown): string {
  if (key === 'retrievedAt' && typeof value === 'string') {
    return new Date(value).toLocaleString('zh-CN');
  }

  if (Array.isArray(value)) {
    return value.join('、');
  }

  return formatValue(value);
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
