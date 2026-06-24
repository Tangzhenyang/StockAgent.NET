# User Data Sources Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add per-user data source configuration and route research collection through configurable market/web data providers.

**Architecture:** Extend the existing user settings JSON storage with a `dataSources` payload. Pass sanitized settings to the frontend and runtime settings with unprotected keys to backend providers. Keep built-in mock providers as fallback, and add custom HTTP provider support for AKShare/TuShare wrapper services.

**Tech Stack:** ASP.NET Core Minimal API, EF Core user settings table, ASP.NET Core Data Protection, `IHttpClientFactory`, React, TanStack Query, TypeScript.

---

### Task 1: Data Source Settings API

**Files:**
- Modify: `src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs`
- Modify: `src/StockAgent.Api/Features/UserSettings/UserSettingsEndpoints.cs`
- Test: `tests/StockAgent.Api.Tests/UserSettingsApiTests.cs`

- [ ] **Step 1: Write failing API test**

Add a test that saves `/api/user-settings/data-sources`, verifies HTTP 200, verifies API keys are not returned, then verifies GET `/api/user-settings` includes `dataSources`.

- [ ] **Step 2: Verify test fails**

Run: `dotnet test --filter UserSettingsApiTests`
Expected: FAIL because `/api/user-settings/data-sources` does not exist.

- [ ] **Step 3: Implement contracts and service**

Add `DataSourceSettingsResponse`, `SaveDataSourceSettingsRequest`, `DataSourceSettingsTestResponse`, and runtime settings with protected API key storage.

- [ ] **Step 4: Implement endpoint validation**

Accept `Mock` and `CustomHttp` providers. Require valid base URLs when `CustomHttp` is selected. Keep API keys optional and never return plaintext or encrypted values.

- [ ] **Step 5: Verify test passes**

Run: `dotnet test --filter UserSettingsApiTests`
Expected: PASS.

### Task 2: Configurable Provider Boundary

**Files:**
- Modify: `src/StockAgent.Api/Infrastructure/DataSources/IMarketDataProvider.cs`
- Modify: `src/StockAgent.Api/Infrastructure/DataSources/IWebResearchProvider.cs`
- Modify: `src/StockAgent.Api/Infrastructure/DataSources/FakeMarketDataProvider.cs`
- Modify: `src/StockAgent.Api/Infrastructure/DataSources/FakeWebResearchProvider.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`
- Create: `src/StockAgent.Api/Infrastructure/DataSources/ConfiguredMarketDataProvider.cs`
- Create: `src/StockAgent.Api/Infrastructure/DataSources/ConfiguredWebResearchProvider.cs`
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs`

- [ ] **Step 1: Write failing orchestration test**

Add test-only providers that assert the orchestrator passes the current user's runtime data source settings into both provider calls.

- [ ] **Step 2: Verify test fails**

Run: `dotnet test --filter ResearchTaskApiTests`
Expected: FAIL because provider interfaces do not receive data source settings.

- [ ] **Step 3: Update provider interfaces**

Add a `DataSourceRuntimeSettings` parameter to both provider interfaces.

- [ ] **Step 4: Implement configurable providers**

For `Mock`, return the existing deterministic data. For `CustomHttp`, call `{baseUrl}/market/snapshot?ticker=...` and `{baseUrl}/web/search?ticker=...&companyName=...` with an optional Bearer token.

- [ ] **Step 5: Verify tests pass**

Run: `dotnet test --filter ResearchTaskApiTests`
Expected: PASS.

### Task 3: Settings Page UI

**Files:**
- Modify: `src/StockAgent.Web/src/models.ts`
- Modify: `src/StockAgent.Web/src/api/settingsApi.ts`
- Modify: `src/StockAgent.Web/src/components/SettingsPage.tsx`
- Modify: `src/StockAgent.Web/src/styles.css`

- [ ] **Step 1: Add TypeScript models**

Add `DataSourceSettings`, `SaveDataSourceSettingsRequest`, and `DataSourceSettingsTestResponse`.

- [ ] **Step 2: Add settings API client functions**

Add `saveDataSourceSettings` and `testDataSourceSettings`.

- [ ] **Step 3: Add UI section**

Add a “数据源配置” panel with provider selectors, base URLs, optional API keys, rate limit, retry count, and a test button.

- [ ] **Step 4: Verify frontend build**

Run: `npm run build` in `src/StockAgent.Web`
Expected: PASS.

### Task 4: Full Verification

**Files:**
- No new files.

- [ ] **Step 1: Run full backend tests**

Run: `dotnet test`
Expected: all tests pass.

- [ ] **Step 2: Run frontend build**

Run: `npm run build`
Expected: build succeeds.

- [ ] **Step 3: Manual smoke test**

Open `http://localhost:5173/settings`, save Mock data source settings, then create a research task and verify it completes.
