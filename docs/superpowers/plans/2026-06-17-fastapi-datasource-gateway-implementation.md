# FastAPI DataSource Gateway Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build an independent FastAPI service that exposes stable market snapshot and evidence search contracts for StockAgent.NET.

**Architecture:** Create `services/stock-datasource-gateway` as a small Python service. The API layer owns routing and security, service modules own contract assembly, provider modules isolate AKShare/public-site dependencies, and tests verify the contract without requiring live external data.

**Tech Stack:** Python 3.12, FastAPI, Pydantic, Uvicorn, pytest, httpx/TestClient, optional AKShare, cachetools.

---

### Task 1: Service Skeleton And Contract Tests

**Files:**
- Create: `services/stock-datasource-gateway/app/__init__.py`
- Create: `services/stock-datasource-gateway/app/main.py`
- Create: `services/stock-datasource-gateway/app/models/contracts.py`
- Create: `services/stock-datasource-gateway/tests/test_health.py`
- Create: `services/stock-datasource-gateway/tests/test_security.py`
- Create: `services/stock-datasource-gateway/tests/test_market_contract.py`
- Create: `services/stock-datasource-gateway/tests/test_evidence_contract.py`
- Create: `services/stock-datasource-gateway/requirements.txt`
- Create: `services/stock-datasource-gateway/README.md`

- [ ] **Step 1: Write failing API tests**

Create tests that expect:

```python
def test_health_returns_service_status(client):
    response = client.get("/api/health")
    assert response.status_code == 200
    assert response.json()["service"] == "stock-datasource-gateway"

def test_market_requires_bearer_token(client):
    response = client.get("/api/market/snapshot?ticker=00700.HK")
    assert response.status_code == 401

def test_market_snapshot_contract(client, auth_headers):
    response = client.get("/api/market/snapshot?ticker=00700.HK", headers=auth_headers)
    assert response.status_code == 200
    body = response.json()
    assert body["ticker"] == "00700.HK"
    assert body["market"] == "HongKong"

def test_evidence_search_contract(client, auth_headers):
    response = client.get("/api/web/search?ticker=00700.HK&companyName=腾讯控股", headers=auth_headers)
    assert response.status_code == 200
    assert response.json()[0]["sourceType"] == "annual-report"
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `python -m pytest`
Expected: FAIL because service files do not exist yet.

- [ ] **Step 3: Implement minimal FastAPI app and Pydantic contracts**

Create `MarketSnapshotResponse`, `EvidenceDocumentResponse`, health route, protected market route, and protected evidence route returning deterministic mock data.

- [ ] **Step 4: Run tests to verify they pass**

Run: `python -m pytest`
Expected: PASS.

### Task 2: Configuration, Security, And Ticker Utilities

**Files:**
- Create: `services/stock-datasource-gateway/app/core/config.py`
- Create: `services/stock-datasource-gateway/app/core/security.py`
- Create: `services/stock-datasource-gateway/app/utils/ticker.py`
- Create: `services/stock-datasource-gateway/tests/test_ticker.py`
- Modify: `services/stock-datasource-gateway/app/main.py`

- [ ] **Step 1: Write failing utility and security tests**

Tests must cover:
- `700` normalizes to `00700.HK`.
- `00700.HK` resolves to `HongKong`.
- `600519` resolves to `AShare`.
- wrong Bearer token returns `401`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `python -m pytest tests/test_ticker.py tests/test_security.py`
Expected: FAIL because utility modules do not exist.

- [ ] **Step 3: Implement config, security dependency, and ticker normalization**

Read `DATA_SOURCE_API_KEY` from environment with default `dev-secret` for local tests. Keep `/api/health` public and protect business routes.

- [ ] **Step 4: Run tests to verify they pass**

Run: `python -m pytest tests/test_ticker.py tests/test_security.py`
Expected: PASS.

### Task 3: Provider And Service Layer

**Files:**
- Create: `services/stock-datasource-gateway/app/providers/akshare_market.py`
- Create: `services/stock-datasource-gateway/app/providers/cninfo_announcements.py`
- Create: `services/stock-datasource-gateway/app/providers/hkex_announcements.py`
- Create: `services/stock-datasource-gateway/app/providers/text_extract.py`
- Create: `services/stock-datasource-gateway/app/services/market_service.py`
- Create: `services/stock-datasource-gateway/app/services/evidence_service.py`
- Modify: `services/stock-datasource-gateway/app/main.py`
- Modify: `services/stock-datasource-gateway/tests/test_market_contract.py`
- Modify: `services/stock-datasource-gateway/tests/test_evidence_contract.py`

- [ ] **Step 1: Write failing service tests**

Tests must assert service functions return the same JSON-compatible shape as the API, and that A-share and Hong Kong tickers route to the correct evidence source type.

- [ ] **Step 2: Run tests to verify they fail**

Run: `python -m pytest tests/test_market_contract.py tests/test_evidence_contract.py`
Expected: FAIL because service/provider modules do not exist.

- [ ] **Step 3: Implement service/provider modules**

Provider modules should expose stable functions. If AKShare is unavailable or returns empty data, the service returns deterministic fallback data and logs the fallback reason.

- [ ] **Step 4: Run tests to verify they pass**

Run: `python -m pytest tests/test_market_contract.py tests/test_evidence_contract.py`
Expected: PASS.

### Task 4: Deployment Artifacts And Final Verification

**Files:**
- Create: `services/stock-datasource-gateway/Dockerfile`
- Create: `services/stock-datasource-gateway/.env.example`
- Modify: `services/stock-datasource-gateway/README.md`

- [ ] **Step 1: Add deployment artifacts**

Add Dockerfile, environment example, and README instructions for configuring StockAgent.NET settings.

- [ ] **Step 2: Run service test suite**

Run: `python -m pytest`
Expected: PASS.

- [ ] **Step 3: Run existing .NET tests**

Run: `dotnet test`
Expected: PASS.

- [ ] **Step 4: Run frontend build**

Run: `npm run build` in `src/StockAgent.Web`
Expected: PASS.
