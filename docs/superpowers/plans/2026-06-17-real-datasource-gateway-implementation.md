# Real DataSource Gateway Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Upgrade the FastAPI datasource gateway from deterministic fallback data to real market, financial, and evidence providers with explicit failure handling.

**Architecture:** Keep the existing `/api/market/snapshot` and `/api/web/search` contracts unchanged for StockAgent.NET. Replace silent fallback behavior with provider functions that call AKShare/CNInfo/HKEX-related sources and raise typed provider errors when real data cannot be obtained. Unit tests monkeypatch provider functions so contract verification stays deterministic without depending on live public sites.

**Tech Stack:** Python 3.12, FastAPI, Pydantic, pytest, AKShare, cachetools.

---

### Task 1: Explicit Provider Errors

**Files:**
- Create: `services/stock-datasource-gateway/app/providers/errors.py`
- Modify: `services/stock-datasource-gateway/app/main.py`
- Test: `services/stock-datasource-gateway/tests/test_provider_errors.py`

- [ ] **Step 1: Write failing tests**

Add tests that monkeypatch `market_service.get_market_snapshot` and `evidence_service.search_evidence_documents` to raise `DataSourceProviderError`, then assert `/api/market/snapshot` and `/api/web/search` return `502`.

- [ ] **Step 2: Run tests to verify they fail**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_provider_errors.py`
Expected: FAIL because `DataSourceProviderError` and exception handlers do not exist.

- [ ] **Step 3: Implement typed provider errors and FastAPI handlers**

Create `DataSourceProviderError(message, provider, retryable=False)` and map it to HTTP `502`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_provider_errors.py`
Expected: PASS.

### Task 2: Real Market Snapshot Provider

**Files:**
- Modify: `services/stock-datasource-gateway/app/providers/akshare_market.py`
- Modify: `services/stock-datasource-gateway/app/services/market_service.py`
- Modify: `services/stock-datasource-gateway/tests/test_market_contract.py`

- [ ] **Step 1: Write failing tests**

Update tests to monkeypatch the market provider with A-share and Hong Kong real-shaped rows and assert the API returns values from the provider rather than deterministic fallback values.

- [ ] **Step 2: Run tests to verify they fail**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_market_contract.py`
Expected: FAIL because the service still uses fallback behavior.

- [ ] **Step 3: Implement real market loading**

Implement `load_market_snapshot(normalized)` using AKShare:
- A Share: try `stock_zh_a_spot_em`, then `stock_zh_a_spot`; enrich from `stock_financial_analysis_indicator_em` or `stock_financial_analysis_indicator`.
- Hong Kong: try `stock_hk_spot_em`, then `stock_hk_spot`; enrich from `stock_hk_financial_indicator_em`.
- Raise `DataSourceProviderError` when no real row is found.

- [ ] **Step 4: Run tests to verify they pass**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_market_contract.py`
Expected: PASS.

### Task 3: Real Evidence Providers

**Files:**
- Modify: `services/stock-datasource-gateway/app/providers/cninfo_announcements.py`
- Modify: `services/stock-datasource-gateway/app/providers/hkex_announcements.py`
- Modify: `services/stock-datasource-gateway/app/services/evidence_service.py`
- Modify: `services/stock-datasource-gateway/tests/test_evidence_contract.py`

- [ ] **Step 1: Write failing tests**

Update evidence tests to monkeypatch A-share and Hong Kong providers with real-shaped announcement rows. Assert A-share returns CNInfo real URL/title/time and Hong Kong returns HKEXnews URL plus real financial evidence text.

- [ ] **Step 2: Run tests to verify they fail**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_evidence_contract.py`
Expected: FAIL because evidence providers still return static fallback text.

- [ ] **Step 3: Implement evidence providers**

Implement:
- A Share: `stock_zh_a_disclosure_report_cninfo` for annual/semiannual/quarterly/announcement categories.
- Hong Kong: `stock_hk_financial_indicator_em` as evidence plus HKEXnews search URL for source traceability.
- Raise `DataSourceProviderError` if no evidence is available.

- [ ] **Step 4: Run tests to verify they pass**

Run: `.\.venv\Scripts\python.exe -m pytest tests/test_evidence_contract.py`
Expected: PASS.

### Task 4: Verification

**Files:**
- Modify: `services/stock-datasource-gateway/README.md`

- [ ] **Step 1: Update README**

Document that real providers are used and deterministic fallback is no longer used for business endpoints.

- [ ] **Step 2: Run gateway tests**

Run: `.\.venv\Scripts\python.exe -m pytest`
Expected: PASS.

- [ ] **Step 3: Run compile check**

Run: `python -m compileall app tests`
Expected: PASS.

- [ ] **Step 4: Run existing project verification**

Run:
- `dotnet test`
- `npm run build` in `src/StockAgent.Web`
Expected: both PASS.
