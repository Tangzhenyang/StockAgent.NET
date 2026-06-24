# Stock DataSource Gateway

独立的 FastAPI 数据源网关，用于给 StockAgent.NET 提供稳定的行情/财务和公告/证据接口。

## 接口

```text
GET /api/health
GET /api/market/snapshot?ticker=00700.HK
GET /api/web/search?ticker=00700.HK&companyName=腾讯控股
```

业务接口需要 Bearer Token：

```http
Authorization: Bearer <DATA_SOURCE_API_KEY>
```

## 本地运行

```bash
python -m venv .venv
source .venv/bin/activate
pip install -r requirements.txt
export DATA_SOURCE_API_KEY="change-me"
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

Windows PowerShell：

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
pip install -r requirements.txt
$env:DATA_SOURCE_API_KEY="change-me"
uvicorn app.main:app --host 0.0.0.0 --port 8000
```

## Docker 部署

```bash
docker build -t stock-datasource-gateway .
docker run -d \
  --name stock-datasource-gateway \
  -p 8000:8000 \
  -e DATA_SOURCE_API_KEY="change-me" \
  stock-datasource-gateway
```

## StockAgent.NET 配置

在 StockAgent.NET 的设置页中填写：

```text
行情/财务数据源：自定义 HTTP 包装服务
行情 Base URL：http://<server>:8000/api
行情 API Key：<DATA_SOURCE_API_KEY>

证据/公告数据源：自定义 HTTP 包装服务
证据 Base URL：http://<server>:8000/api
证据 API Key：<DATA_SOURCE_API_KEY>
```

行情接口会调用：

```text
GET http://<server>:8000/api/market/snapshot?ticker=00700.HK
```

证据接口会调用：

```text
GET http://<server>:8000/api/web/search?ticker=00700.HK&companyName=腾讯控股
```

## 当前实现状态

当前版本提供完整 FastAPI 契约、鉴权、Ticker 规范化、缓存和真实数据源访问。

行情/财务 Provider 会通过 AKShare 尝试读取：

- A 股实时行情：东方财富接口，失败后尝试新浪接口
- A 股财务指标：东方财富/新浪财务指标接口
- 港股实时行情：东方财富接口，失败后尝试新浪接口
- 港股财务指标：东方财富港股财务指标接口

公告/证据 Provider 会通过 AKShare 和公开入口尝试读取：

- A 股公告：巨潮资讯公告查询，返回真实公告标题、时间、链接
- 港股证据：港股真实财务指标文本 + HKEXnews 股票代码检索入口

如果真实数据源不可用、接口变化、网络超时或返回空结果，业务接口会返回 `502`，不会再静默返回模拟数据。这样可以避免研究报告把兜底文本误当成真实证据。

后续如果需要更深的证据正文，可以在 `providers/text_extract.py` 中增加公告 PDF/HTML 正文抽取。

## 测试

```bash
pytest
```

如果当前机器无法访问 PyPI，可以先用语法检查：

```bash
python -m compileall app tests
```
