import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { FormEvent } from 'react';
import { useEffect, useState } from 'react';
import {
  getUserSettings,
  saveDataSourceSettings,
  saveModelSettings,
  saveResearchSettings,
  testDataSourceSettings,
  testModelSettings,
} from '../api/settingsApi';
import type { SaveDataSourceSettingsRequest, SaveResearchSettingsRequest } from '../models';

/**
 * Editable user settings page for model and research configuration.
 */
export function SettingsPage() {
  const queryClient = useQueryClient();
  const settingsQuery = useQuery({
    queryKey: ['userSettings'],
    queryFn: getUserSettings,
  });
  const [provider, setProvider] = useState('OpenAICompatible');
  const [baseUrl, setBaseUrl] = useState('');
  const [model, setModel] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [research, setResearch] = useState<SaveResearchSettingsRequest>({
    defaultLanguage: 'zh-CN',
    maxEvidenceCards: 30,
    maxDocumentChunks: 300,
    maxRetrievedChunks: 30,
    retainRawDocuments: false,
  });
  const [dataSources, setDataSources] = useState<SaveDataSourceSettingsRequest>({
    officialAnnouncementsEnabled: true,
    newsSearchEnabled: true,
    marketDataProvider: 'Mock',
    marketDataBaseUrl: '',
    webResearchProvider: 'Mock',
    webResearchBaseUrl: '',
    maxRequestsPerMinute: 30,
    retryCount: 2,
  });
  const [marketDataApiKey, setMarketDataApiKey] = useState('');
  const [webResearchApiKey, setWebResearchApiKey] = useState('');

  useEffect(() => {
    if (!settingsQuery.data) {
      return;
    }

    setProvider(settingsQuery.data.model.provider || 'OpenAICompatible');
    setBaseUrl(settingsQuery.data.model.baseUrl);
    setModel(settingsQuery.data.model.model);
    setResearch(settingsQuery.data.research);
    setDataSources({
      officialAnnouncementsEnabled: settingsQuery.data.dataSources.officialAnnouncementsEnabled,
      newsSearchEnabled: settingsQuery.data.dataSources.newsSearchEnabled,
      marketDataProvider: settingsQuery.data.dataSources.marketDataProvider,
      marketDataBaseUrl: settingsQuery.data.dataSources.marketDataBaseUrl,
      webResearchProvider: settingsQuery.data.dataSources.webResearchProvider,
      webResearchBaseUrl: settingsQuery.data.dataSources.webResearchBaseUrl,
      maxRequestsPerMinute: settingsQuery.data.dataSources.maxRequestsPerMinute,
      retryCount: settingsQuery.data.dataSources.retryCount,
    });
  }, [settingsQuery.data]);

  const saveModelMutation = useMutation({
    mutationFn: () => saveModelSettings({ provider, baseUrl, model, apiKey: apiKey.trim() || undefined }),
    onSuccess: async () => {
      setApiKey('');
      await queryClient.invalidateQueries({ queryKey: ['userSettings'] });
    },
  });
  const saveResearchMutation = useMutation({
    mutationFn: () => saveResearchSettings(research),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['userSettings'] });
    },
  });
  const saveDataSourceMutation = useMutation({
    mutationFn: () =>
      saveDataSourceSettings({
        ...dataSources,
        marketDataApiKey: marketDataApiKey.trim() || undefined,
        webResearchApiKey: webResearchApiKey.trim() || undefined,
      }),
    onSuccess: async () => {
      setMarketDataApiKey('');
      setWebResearchApiKey('');
      await queryClient.invalidateQueries({ queryKey: ['userSettings'] });
    },
  });
  const testModelMutation = useMutation({
    mutationFn: testModelSettings,
  });
  const testDataSourceMutation = useMutation({
    mutationFn: testDataSourceSettings,
  });

  const handleModelSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveModelMutation.mutate();
  };

  const handleResearchSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveResearchMutation.mutate();
  };

  const handleDataSourceSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    saveDataSourceMutation.mutate();
  };

  return (
    <main className="settingsPage">
      <section className="settingsHeader">
        <h1>设置</h1>
        <p className="muted">模型密钥保存后不会回显，只显示是否已配置。</p>
      </section>

      <div className="settingsGrid">
        <form className="settingsPanel" onSubmit={handleModelSubmit}>
          <h2>大模型 API</h2>
          <label>
            提供商
            <select value={provider} onChange={(event) => setProvider(event.target.value)}>
              <option value="OpenAICompatible">OpenAI Compatible</option>
              <option value="OpenAI">OpenAI</option>
              <option value="AzureOpenAI">Azure OpenAI</option>
            </select>
          </label>
          <label>
            Base URL
            <input value={baseUrl} onChange={(event) => setBaseUrl(event.target.value)} placeholder="https://api.example.com/v1" />
          </label>
          <label>
            模型
            <input value={model} onChange={(event) => setModel(event.target.value)} placeholder="deep-research-model" />
          </label>
          <label>
            API Key
            <input
              type="password"
              value={apiKey}
              onChange={(event) => setApiKey(event.target.value)}
              placeholder={settingsQuery.data?.model.apiKeyConfigured ? '留空则保留当前密钥' : '请输入 API Key'}
              autoComplete="off"
            />
          </label>
          <div className="settingsStatus">
            <span>密钥状态</span>
            <strong>{settingsQuery.data?.model.apiKeyConfigured ? '已配置' : '未配置'}</strong>
          </div>
          <div className="formActions">
            <button type="submit" disabled={saveModelMutation.isPending || !provider || !baseUrl || !model}>
              {saveModelMutation.isPending ? '保存中' : '保存模型配置'}
            </button>
            <button type="button" className="secondaryButton" onClick={() => testModelMutation.mutate()}>
              测试连接
            </button>
          </div>
          {saveModelMutation.isSuccess && <p className="formSuccess">模型配置已保存。</p>}
          {saveModelMutation.isError && <p className="formError">模型配置保存失败。</p>}
          {testModelMutation.data && (
            <p className={testModelMutation.data.succeeded ? 'formSuccess' : 'formError'}>
              {testModelMutation.data.message}
            </p>
          )}
        </form>

        <form className="settingsPanel" onSubmit={handleResearchSubmit}>
          <h2>研究配置</h2>
          <label>
            报告语言
            <select
              value={research.defaultLanguage}
              onChange={(event) => setResearch((value) => ({ ...value, defaultLanguage: event.target.value }))}
            >
              <option value="zh-CN">中文</option>
              <option value="en-US">English</option>
            </select>
          </label>
          <label>
            证据上限
            <input
              type="number"
              min={1}
              max={200}
              value={research.maxEvidenceCards}
              onChange={(event) =>
                setResearch((value) => ({ ...value, maxEvidenceCards: Number(event.target.value) }))
              }
            />
          </label>
          <label>
            文档分块上限
            <input
              type="number"
              min={1}
              max={5000}
              value={research.maxDocumentChunks}
              onChange={(event) =>
                setResearch((value) => ({ ...value, maxDocumentChunks: Number(event.target.value) }))
              }
            />
          </label>
          <label>
            检索片段上限
            <input
              type="number"
              min={1}
              max={1000}
              value={research.maxRetrievedChunks}
              onChange={(event) =>
                setResearch((value) => ({ ...value, maxRetrievedChunks: Number(event.target.value) }))
              }
            />
          </label>
          <label className="checkboxLabel">
            <input
              type="checkbox"
              checked={research.retainRawDocuments}
              onChange={(event) =>
                setResearch((value) => ({ ...value, retainRawDocuments: event.target.checked }))
              }
            />
            保留原始文档
          </label>
          <button type="submit" disabled={saveResearchMutation.isPending}>
            {saveResearchMutation.isPending ? '保存中' : '保存研究配置'}
          </button>
          {saveResearchMutation.isSuccess && <p className="formSuccess">研究配置已保存。</p>}
          {saveResearchMutation.isError && <p className="formError">研究配置保存失败。</p>}
        </form>

        <form className="settingsPanel" onSubmit={handleDataSourceSubmit}>
          <h2>数据源配置</h2>
          <label className="checkboxLabel">
            <input
              type="checkbox"
              checked={dataSources.officialAnnouncementsEnabled}
              onChange={(event) =>
                setDataSources((value) => ({ ...value, officialAnnouncementsEnabled: event.target.checked }))
              }
            />
            启用官方公告源
          </label>
          <label className="checkboxLabel">
            <input
              type="checkbox"
              checked={dataSources.newsSearchEnabled}
              onChange={(event) => setDataSources((value) => ({ ...value, newsSearchEnabled: event.target.checked }))}
            />
            启用新闻搜索源
          </label>
          <label>
            行情/财务数据源
            <select
              value={dataSources.marketDataProvider}
              onChange={(event) =>
                setDataSources((value) => ({
                  ...value,
                  marketDataProvider: event.target.value as SaveDataSourceSettingsRequest['marketDataProvider'],
                }))
              }
            >
              <option value="Mock">Mock 内置数据</option>
              <option value="CustomHttp">自定义 HTTP 包装服务</option>
            </select>
          </label>
          <label>
            行情 Base URL
            <input
              value={dataSources.marketDataBaseUrl}
              onChange={(event) => setDataSources((value) => ({ ...value, marketDataBaseUrl: event.target.value }))}
              placeholder="https://provider.example.com/api"
            />
          </label>
          <label>
            行情 API Key
            <input
              type="password"
              value={marketDataApiKey}
              onChange={(event) => setMarketDataApiKey(event.target.value)}
              placeholder={settingsQuery.data?.dataSources.marketDataApiKeyConfigured ? '留空则保留当前密钥' : '可选'}
              autoComplete="off"
            />
          </label>
          <div className="settingsStatus">
            <span>行情密钥</span>
            <strong>{settingsQuery.data?.dataSources.marketDataApiKeyConfigured ? '已配置' : '未配置'}</strong>
          </div>
          <label>
            证据/公告数据源
            <select
              value={dataSources.webResearchProvider}
              onChange={(event) =>
                setDataSources((value) => ({
                  ...value,
                  webResearchProvider: event.target.value as SaveDataSourceSettingsRequest['webResearchProvider'],
                }))
              }
            >
              <option value="Mock">Mock 内置证据</option>
              <option value="CustomHttp">自定义 HTTP 包装服务</option>
            </select>
          </label>
          <label>
            证据 Base URL
            <input
              value={dataSources.webResearchBaseUrl}
              onChange={(event) => setDataSources((value) => ({ ...value, webResearchBaseUrl: event.target.value }))}
              placeholder="https://research.example.com/api"
            />
          </label>
          <label>
            证据 API Key
            <input
              type="password"
              value={webResearchApiKey}
              onChange={(event) => setWebResearchApiKey(event.target.value)}
              placeholder={settingsQuery.data?.dataSources.webResearchApiKeyConfigured ? '留空则保留当前密钥' : '可选'}
              autoComplete="off"
            />
          </label>
          <div className="settingsStatus">
            <span>证据密钥</span>
            <strong>{settingsQuery.data?.dataSources.webResearchApiKeyConfigured ? '已配置' : '未配置'}</strong>
          </div>
          <label>
            每分钟请求数
            <input
              type="number"
              min={1}
              max={600}
              value={dataSources.maxRequestsPerMinute}
              onChange={(event) =>
                setDataSources((value) => ({ ...value, maxRequestsPerMinute: Number(event.target.value) }))
              }
            />
          </label>
          <label>
            失败重试次数
            <input
              type="number"
              min={0}
              max={5}
              value={dataSources.retryCount}
              onChange={(event) => setDataSources((value) => ({ ...value, retryCount: Number(event.target.value) }))}
            />
          </label>
          <div className="formActions">
            <button type="submit" disabled={saveDataSourceMutation.isPending}>
              {saveDataSourceMutation.isPending ? '保存中' : '保存数据源配置'}
            </button>
            <button type="button" className="secondaryButton" onClick={() => testDataSourceMutation.mutate()}>
              测试配置
            </button>
          </div>
          {saveDataSourceMutation.isSuccess && <p className="formSuccess">数据源配置已保存。</p>}
          {saveDataSourceMutation.isError && <p className="formError">数据源配置保存失败。</p>}
          {testDataSourceMutation.data && (
            <p className={testDataSourceMutation.data.succeeded ? 'formSuccess' : 'formError'}>
              {testDataSourceMutation.data.message}
            </p>
          )}
        </form>
      </div>
    </main>
  );
}
