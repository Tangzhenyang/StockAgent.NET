# FastAPI 数据源网关设计

## 目标

为 StockAgent.NET 增加一个独立的 Python FastAPI 数据源服务，作为 .NET 主应用和 AKShare、公告网站、PDF 文本提取逻辑之间的稳定适配层。主应用只依赖两个固定接口：

- `GET /api/market/snapshot?ticker=00700.HK`
- `GET /api/web/search?ticker=00700.HK&companyName=腾讯控股`

这样后续无论底层使用 AKShare、TuShare、巨潮资讯、HKEXnews，还是自建爬取逻辑，.NET 主项目都不需要频繁修改。

## 范围

首版服务只覆盖 A 股和港股研究所需的最小闭环：

- 行情和财务快照：公司名、市场、最新价格、市值、市盈率、收入增长率、净利率。
- 公告和证据文档：公告 URL、标题、类型、发布时间、正文文本。
- Bearer Token 鉴权。
- 简单缓存，减少重复请求。
- 健康检查接口。

首版不做用户系统、不保存长期研究历史、不直接接入主项目数据库。它是一个无状态或轻状态的外部数据服务。

## 外部接口

### 健康检查

```http
GET /api/health
```

响应：

```json
{
  "status": "ok",
  "service": "stock-datasource-gateway"
}
```

### 行情/财务快照

```http
GET /api/market/snapshot?ticker=00700.HK
Authorization: Bearer <DATA_SOURCE_API_KEY>
```

响应必须与 .NET 的 `MarketDataSnapshot` 对齐：

```json
{
  "ticker": "00700.HK",
  "market": "HongKong",
  "companyName": "腾讯控股",
  "lastPrice": 320.5,
  "marketCap": 3000000000000,
  "peRatio": 18.4,
  "revenueGrowthPercent": 8.2,
  "netMarginPercent": 24.5
}
```

市场枚举只返回：

- `AShare`
- `HongKong`

### 公告/证据检索

```http
GET /api/web/search?ticker=00700.HK&companyName=腾讯控股
Authorization: Bearer <DATA_SOURCE_API_KEY>
```

响应必须与 .NET 的 `WebEvidenceDocument[]` 对齐：

```json
[
  {
    "url": "https://www1.hkexnews.hk/listedco/listconews/sehk/2026/example.pdf",
    "title": "腾讯控股 年度报告",
    "sourceType": "annual-report",
    "publishedAt": "2026-03-20T00:00:00+00:00",
    "text": "从公告或 PDF 中提取出来的正文文本..."
  }
]
```

`sourceType` 首版使用以下值：

- `annual-report`
- `interim-report`
- `quarterly-report`
- `announcement`
- `regulatory`
- `news`

## 内部模块

```text
services/stock-datasource-gateway/
  app/
    main.py
    core/
      config.py
      security.py
      cache.py
    models/
      contracts.py
    providers/
      akshare_market.py
      cninfo_announcements.py
      hkex_announcements.py
      text_extract.py
    services/
      market_service.py
      evidence_service.py
    utils/
      ticker.py
  tests/
    test_health.py
    test_security.py
    test_market_contract.py
    test_evidence_contract.py
  requirements.txt
  Dockerfile
  README.md
```

### `main.py`

创建 FastAPI 应用，注册 `/api/health`、`/api/market/snapshot`、`/api/web/search`。

### `core/config.py`

从环境变量读取配置：

- `DATA_SOURCE_API_KEY`
- `CACHE_TTL_MARKET_SECONDS`
- `CACHE_TTL_EVIDENCE_SECONDS`
- `REQUEST_TIMEOUT_SECONDS`
- `MAX_EVIDENCE_DOCUMENTS`

### `core/security.py`

校验 `Authorization: Bearer ...`。除 `/api/health` 外，所有接口必须鉴权。

### `providers/akshare_market.py`

封装 AKShare 行情和财务数据调用。服务层不直接依赖 AKShare 的原始字段名，避免 AKShare 返回结构变化影响外层契约。

### `providers/cninfo_announcements.py`

负责 A 股公告检索，首版优先从 AKShare 可用接口或巨潮公开入口获取公告列表。

### `providers/hkex_announcements.py`

负责港股公告检索，首版优先 HKEXnews 公告入口。

### `providers/text_extract.py`

负责 PDF/HTML 文本抽取。首版提取前若干页或限制最大字符数，避免单个 PDF 占用过多内存。

### `services/market_service.py`

组合行情、估值、财务指标，输出标准 `MarketSnapshotResponse`。

### `services/evidence_service.py`

组合公告列表、正文提取、证据分类，输出标准 `EvidenceDocumentResponse[]`。

## 技术栈

- Python 3.11+
- FastAPI
- Uvicorn
- Pydantic
- AKShare
- pandas
- httpx
- pdfplumber 或 pypdf
- beautifulsoup4
- cachetools 或 diskcache
- pytest
- ruff

AKShare 官方 HTTP 部署文档说明它可通过 AKTools 以 FastAPI/Uvicorn 方式暴露 HTTP 服务。本设计不直接依赖 AKTools 的外部接口，而是用 AKShare 作为内部数据能力，外层提供更稳定、更小的业务接口。

参考：

- https://akshare.akfamily.xyz/deploy_http.html
- https://akshare.akfamily.xyz/data/stock/stock.html

## 数据流

### 行情/财务

1. .NET 调用 `/api/market/snapshot?ticker=00700.HK`。
2. FastAPI 校验 Bearer Token。
3. `ticker.py` 规范化股票代码和市场。
4. `market_service.py` 检查缓存。
5. 缓存未命中时调用 `akshare_market.py`。
6. 服务层将 AKShare 原始结果映射为 `MarketSnapshotResponse`。
7. 返回给 .NET。

### 公告/证据

1. .NET 调用 `/api/web/search?ticker=00700.HK&companyName=腾讯控股`。
2. FastAPI 校验 Bearer Token。
3. `ticker.py` 识别 A 股或港股。
4. A 股走 `cninfo_announcements.py`，港股走 `hkex_announcements.py`。
5. 获取公告 URL 和标题后，`text_extract.py` 抽取正文。
6. `evidence_service.py` 做类型归类、数量限制、文本裁剪。
7. 返回 `WebEvidenceDocument[]`。

## 缓存策略

- 行情/财务快照：缓存 1 到 5 分钟。
- 财务指标：缓存 12 到 24 小时。
- 公告列表：缓存 1 到 6 小时。
- PDF/HTML 正文提取：缓存 7 到 30 天。

首版可以使用进程内 `cachetools`。如果后续服务重启频繁或公告 PDF 提取成本较高，再切换 `diskcache`。

## 错误处理

- 未授权：返回 `401`。
- 股票代码格式错误：返回 `400`。
- 底层数据源超时：返回 `504`。
- 底层数据源返回空结果：返回 `404` 或降级为空数组。
- 单个公告正文提取失败：跳过该公告并记录日志，不让整个 `/api/web/search` 失败。

## 安全

- 所有业务接口必须启用 Bearer Token。
- 不在日志中输出 API Key。
- 不建议公网裸露服务；优先内网访问或 Nginx 反代加 IP 白名单。
- Nginx 层建议启用 HTTPS、请求体限制、基础限流。

## 部署

首版支持两种部署方式：

### 直接运行

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
export DATA_SOURCE_API_KEY="change-me"
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

### Docker

```bash
docker build -t stock-datasource-gateway .
docker run -d \
  --name stock-datasource-gateway \
  -p 8000:8000 \
  -e DATA_SOURCE_API_KEY="change-me" \
  stock-datasource-gateway
```

.NET 设置页填写：

```text
行情/财务数据源：自定义 HTTP 包装服务
行情 Base URL：http://<server>:8000/api
行情 API Key：<DATA_SOURCE_API_KEY>

证据/公告数据源：自定义 HTTP 包装服务
证据 Base URL：http://<server>:8000/api
证据 API Key：<DATA_SOURCE_API_KEY>
```

## 测试策略

- 单元测试：
  - 鉴权成功/失败。
  - 股票代码规范化。
  - 响应模型 JSON 字段与 .NET 契约一致。
  - AKShare provider 原始字段映射。
  - 公告 provider 空结果和异常降级。

- 集成测试：
  - `GET /api/health` 返回 200。
  - 无 Bearer Token 调用业务接口返回 401。
  - 使用 Mock provider 时 `/api/market/snapshot` 返回标准结构。
  - 使用 Mock provider 时 `/api/web/search` 返回标准数组。

## 首版限制

- 数据质量依赖 AKShare 和公开网站可用性。
- 公告 PDF 正文抽取先做文本级提取，不做 OCR。
- 不做分布式限流。
- 不长期保存原始公告文件。
- 不直接生成向量，向量化仍由 .NET 主项目或后续独立服务负责。
