# User Auth Settings History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add persistent user login, per-user model/research settings, report history, and a clearer non-button task progress stepper.

**Architecture:** Use ASP.NET Core Identity with persistent HttpOnly application cookies for authentication. Store user-owned settings and research tasks in PostgreSQL through EF Core, and keep all business endpoints scoped to the current user. The React app restores login state from `/api/auth/me`, uses cookie credentials for API calls, and separates workbench, history, and settings views.

**Tech Stack:** .NET 10, ASP.NET Core Minimal API, ASP.NET Core Identity, EF Core, Npgsql/PostgreSQL, ASP.NET Core Data Protection, xUnit, WebApplicationFactory, React, TypeScript, Vite, TanStack Query, React Router.

---

## Scope Check

The feature spans backend authentication, data ownership, settings persistence, history views, and frontend navigation. These pieces are coupled by the user boundary, so the plan implements one vertical slice rather than separate subsystem plans. Live model invocation remains behind the existing Semantic Kernel boundary; this plan stores model configuration and prepares the analysis service to receive user settings, but it does not replace the deterministic test analysis with a paid provider call.

## File Structure

Create these backend files:

```text
src/StockAgent.Api/Domain/ApplicationUser.cs
src/StockAgent.Api/Domain/UserSetting.cs
src/StockAgent.Api/Features/Auth/AuthContracts.cs
src/StockAgent.Api/Features/Auth/AuthEndpoints.cs
src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs
src/StockAgent.Api/Features/UserSettings/UserSettingsEndpoints.cs
src/StockAgent.Api/Infrastructure/Security/CurrentUser.cs
src/StockAgent.Api/Infrastructure/Security/ICurrentUser.cs
src/StockAgent.Api/Infrastructure/Security/IApiKeyProtector.cs
src/StockAgent.Api/Infrastructure/Security/DataProtectionApiKeyProtector.cs
src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs
```

Modify these backend files:

```text
src/StockAgent.Api/StockAgent.Api.csproj
src/StockAgent.Api/Program.cs
src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs
src/StockAgent.Api/Domain/ResearchTask.cs
src/StockAgent.Api/Features/ResearchTasks/ResearchTaskContracts.cs
src/StockAgent.Api/Features/ResearchTasks/ResearchTaskEndpoints.cs
src/StockAgent.Api/Features/Reports/ReportEndpoints.cs
src/StockAgent.Api/Features/Evidence/EvidenceEndpoints.cs
src/StockAgent.Api/Features/Pdf/PdfEndpoints.cs
src/StockAgent.Api/Features/Health/DataSourceHealthEndpoints.cs
src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs
```

Create or modify these tests:

```text
tests/StockAgent.Api.Tests/AuthApiTests.cs
tests/StockAgent.Api.Tests/UserSettingsApiTests.cs
tests/StockAgent.Api.Tests/UserScopedResearchApiTests.cs
tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs
tests/StockAgent.Api.Tests/StockAgentDbContextTests.cs
```

Create these frontend files:

```text
src/StockAgent.Web/src/api/authApi.ts
src/StockAgent.Web/src/api/settingsApi.ts
src/StockAgent.Web/src/components/AppShell.tsx
src/StockAgent.Web/src/components/AuthGuard.tsx
src/StockAgent.Web/src/components/LoginPage.tsx
src/StockAgent.Web/src/components/RegisterPage.tsx
src/StockAgent.Web/src/components/HistoryPage.tsx
```

Modify these frontend files:

```text
src/StockAgent.Web/src/api/researchApi.ts
src/StockAgent.Web/src/App.tsx
src/StockAgent.Web/src/components/ResearchWorkbench.tsx
src/StockAgent.Web/src/components/SettingsPage.tsx
src/StockAgent.Web/src/components/TaskTimeline.tsx
src/StockAgent.Web/src/models.ts
src/StockAgent.Web/src/styles.css
```

---

### Task 1: Backend Identity And Persistence Foundation

**Files:**
- Modify: `src/StockAgent.Api/StockAgent.Api.csproj`
- Create: `src/StockAgent.Api/Domain/ApplicationUser.cs`
- Create: `src/StockAgent.Api/Domain/UserSetting.cs`
- Modify: `src/StockAgent.Api/Domain/ResearchTask.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs`
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/StockAgentDbContextTests.cs`

- [ ] **Step 1: Write the failing persistence test**

Add a test that creates a user, a user setting, and a research task with `UserId`, then verifies they can be loaded back by user ID.

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter StockAgentDbContextTests`

Expected: FAIL because `ApplicationUser`, `UserSetting`, and `ResearchTask.UserId` do not exist yet.

- [ ] **Step 2: Add Identity package and domain models**

Add `Microsoft.AspNetCore.Identity.EntityFrameworkCore` to the API project. Create `ApplicationUser` with XML comments, create `UserSetting` with `UserId`, `SettingKey`, `SettingValueJson`, and `UpdatedAt`, and add `UserId` to `ResearchTask`.

- [ ] **Step 3: Update DbContext**

Make `StockAgentDbContext` inherit from `IdentityDbContext<ApplicationUser>`, add `DbSet<UserSetting>`, configure user setting uniqueness on `{ UserId, SettingKey }`, and index `ResearchTask.UserId`.

- [ ] **Step 4: Configure Identity and auth middleware**

Register Identity, cookie auth, authorization, and Data Protection in `Program.cs`. Add `app.UseAuthentication()` before `app.UseAuthorization()`. Keep health and auth endpoints anonymous; protect business endpoints in later tasks.

- [ ] **Step 5: Verify foundation**

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter StockAgentDbContextTests`

Expected: PASS.

---

### Task 2: Auth API With Persistent Cookie

**Files:**
- Create: `src/StockAgent.Api/Features/Auth/AuthContracts.cs`
- Create: `src/StockAgent.Api/Features/Auth/AuthEndpoints.cs`
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/AuthApiTests.cs`

- [ ] **Step 1: Write failing auth endpoint tests**

Add tests for registration, login, `/api/auth/me`, and logout. The login test must verify the response includes an auth cookie and that a later `/api/auth/me` request returns the username when using the same client cookie container.

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter AuthApiTests`

Expected: FAIL because `/api/auth/*` endpoints do not exist.

- [ ] **Step 2: Add contracts**

Create request/response records for register, login, current user, and API errors. Use email or username consistently as the sign-in name.

- [ ] **Step 3: Implement endpoints**

Use `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`. Login uses `AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30) }`. `/api/auth/me` returns 401 when anonymous and current user data when authenticated.

- [ ] **Step 4: Map auth endpoints**

Call `app.MapAuthEndpoints()` in `Program.cs`. Do not require authorization for register, login, logout, and me; `me` handles anonymous status explicitly.

- [ ] **Step 5: Verify auth**

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter AuthApiTests`

Expected: PASS.

---

### Task 3: User Settings And API Key Protection

**Files:**
- Create: `src/StockAgent.Api/Features/UserSettings/UserSettingsContracts.cs`
- Create: `src/StockAgent.Api/Features/UserSettings/UserSettingsEndpoints.cs`
- Create: `src/StockAgent.Api/Infrastructure/Security/IApiKeyProtector.cs`
- Create: `src/StockAgent.Api/Infrastructure/Security/DataProtectionApiKeyProtector.cs`
- Create: `src/StockAgent.Api/Infrastructure/Settings/UserSettingsService.cs`
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/UserSettingsApiTests.cs`

- [ ] **Step 1: Write failing settings tests**

Add tests that an anonymous user receives 401, an authenticated user can save model and research settings, and the returned model settings show `apiKeyConfigured = true` without returning the API key.

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter UserSettingsApiTests`

Expected: FAIL because `/api/user-settings` endpoints do not exist.

- [ ] **Step 2: Implement API key protector**

Wrap ASP.NET Core Data Protection with `Protect` and `Unprotect` methods. Use a stable purpose string such as `StockAgent.UserModelApiKey.v1`.

- [ ] **Step 3: Implement user settings service**

Read and write `UserSetting` rows for keys `model` and `research`. Preserve the old encrypted API key when the request omits a new key. Return sanitized DTOs only.

- [ ] **Step 4: Implement settings endpoints**

Map `GET /api/user-settings`, `PUT /api/user-settings/model`, `PUT /api/user-settings/research`, and `POST /api/user-settings/model/test`. Require authorization for the group. The model test can validate that Base URL, model, and API Key are present in this version.

- [ ] **Step 5: Verify settings**

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter UserSettingsApiTests`

Expected: PASS.

---

### Task 4: User-Scoped Research, Reports, Evidence, And PDF

**Files:**
- Modify: `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskContracts.cs`
- Modify: `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskEndpoints.cs`
- Modify: `src/StockAgent.Api/Features/Reports/ReportEndpoints.cs`
- Modify: `src/StockAgent.Api/Features/Evidence/EvidenceEndpoints.cs`
- Modify: `src/StockAgent.Api/Features/Pdf/PdfEndpoints.cs`
- Modify: `src/StockAgent.Api/Features/Health/DataSourceHealthEndpoints.cs`
- Modify: `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`
- Test: `tests/StockAgent.Api.Tests/UserScopedResearchApiTests.cs`

- [ ] **Step 1: Write failing authorization tests**

Add tests that anonymous research API calls return 401, user A cannot see user B's tasks, and user A receives 404 for user B's report/evidence/PDF endpoints.

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter UserScopedResearchApiTests`

Expected: FAIL because current endpoints are public and global.

- [ ] **Step 2: Attach current user to new tasks**

Require authorization on research task endpoints. On create, set `ResearchTask.UserId` to the current user ID. On list, filter by current user ID and optionally support `status=completed`.

- [ ] **Step 3: Scope report, evidence, and PDF endpoints**

Before returning associated data, confirm the parent `ResearchTask` belongs to the current user. Return 404 for missing or foreign tasks.

- [ ] **Step 4: Read research settings in the pipeline**

When selecting evidence, use the current task owner's research configuration for `maxEvidenceCards`. If settings are absent, use the default value `30`.

- [ ] **Step 5: Verify research isolation**

Run: `dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter UserScopedResearchApiTests`

Expected: PASS.

---

### Task 5: Frontend Auth Shell And API Credentials

**Files:**
- Create: `src/StockAgent.Web/src/api/authApi.ts`
- Modify: `src/StockAgent.Web/src/api/researchApi.ts`
- Create: `src/StockAgent.Web/src/components/AppShell.tsx`
- Create: `src/StockAgent.Web/src/components/AuthGuard.tsx`
- Create: `src/StockAgent.Web/src/components/LoginPage.tsx`
- Create: `src/StockAgent.Web/src/components/RegisterPage.tsx`
- Modify: `src/StockAgent.Web/src/App.tsx`
- Modify: `src/StockAgent.Web/src/models.ts`

- [ ] **Step 1: Add frontend auth API types**

Create `CurrentUser`, `LoginRequest`, and `RegisterRequest` types. Every API call must use `credentials: 'include'`.

- [ ] **Step 2: Add login and register pages**

Build compact form pages with username/email and password inputs. Login success invalidates the `currentUser` query and routes to `/`.

- [ ] **Step 3: Add protected app shell**

Use React Router routes for `/login`, `/register`, `/`, `/history`, and `/settings`. `AuthGuard` calls `/api/auth/me`; while loading, show a small loading state; when 401, route to `/login`.

- [ ] **Step 4: Update research API fetch calls**

Add `credentials: 'include'` to create/list/report/evidence/PDF requests so cookie login works after refresh.

- [ ] **Step 5: Verify frontend build**

Run: `npm run build` from `src/StockAgent.Web`.

Expected: PASS.

---

### Task 6: Frontend Settings And History Pages

**Files:**
- Create: `src/StockAgent.Web/src/api/settingsApi.ts`
- Modify: `src/StockAgent.Web/src/components/SettingsPage.tsx`
- Create: `src/StockAgent.Web/src/components/HistoryPage.tsx`
- Modify: `src/StockAgent.Web/src/components/ResearchWorkbench.tsx`
- Modify: `src/StockAgent.Web/src/models.ts`

- [ ] **Step 1: Add settings API client**

Implement `getUserSettings`, `saveModelSettings`, `saveResearchSettings`, and `testModelSettings`, all with `credentials: 'include'`.

- [ ] **Step 2: Replace static settings panel**

Turn `SettingsPage` into an editable page with model provider, Base URL, model, API Key password input, report language, evidence limit, and save/test actions. Display API Key status as configured/not configured.

- [ ] **Step 3: Add history page**

Use `listResearchTasks('completed')` to show completed tasks. Let the user select a historical task and render its report/evidence through the existing viewer components.

- [ ] **Step 4: Keep workbench focused**

Remove the static settings panel from the right rail. Keep evidence and report reading in the workbench; settings live under `/settings`.

- [ ] **Step 5: Verify frontend build**

Run: `npm run build` from `src/StockAgent.Web`.

Expected: PASS.

---

### Task 7: Stepper Timeline Styling

**Files:**
- Modify: `src/StockAgent.Web/src/components/TaskTimeline.tsx`
- Modify: `src/StockAgent.Web/src/styles.css`

- [ ] **Step 1: Update timeline semantics**

Keep the ordered list but add per-step state classes: `done`, `current`, `pending`, and `failed`. Add `aria-current="step"` to the current stage.

- [ ] **Step 2: Replace button-like CSS**

Use circles, connector lines, subdued labels, and no pointer cursor. Avoid hover styles and button borders.

- [ ] **Step 3: Verify responsive layout**

Run: `npm run build` from `src/StockAgent.Web`.

Expected: PASS.

---

### Task 8: End-To-End Verification

**Files:**
- Modify only files required by prior failed verifications.

- [ ] **Step 1: Run backend tests**

Run: `dotnet test`

Expected: PASS.

- [ ] **Step 2: Run frontend build**

Run: `npm run build` from `src/StockAgent.Web`.

Expected: PASS.

- [ ] **Step 3: Run API and frontend locally**

Start the API with the configured PostgreSQL connection string. Start Vite on an available local port. Verify register, login, settings save, task creation, report history, PDF export, logout, and login persistence.

- [ ] **Step 4: Review git diff**

Run: `git diff --stat` and inspect key changed files. Confirm unrelated existing edits were not reverted.

