# Stock Research Agent Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the first runnable MVP of the .NET 10 A-share/Hong Kong stock research agent with a React workbench, durable research tasks, Semantic Kernel-ready orchestration, evidence/context controls, Chinese scoring reports, and PDF export.

**Architecture:** Use a modular monolith backend plus separate React frontend. ASP.NET Core owns durable task state, queueing, persistence, report generation, and PDF export; Semantic Kernel is introduced behind a narrow AI-analysis boundary so providers remain replaceable. External data/model calls are represented by interfaces and deterministic fake implementations in this first execution plan, giving the project a testable vertical slice before real provider wiring.

**Tech Stack:** .NET 10, ASP.NET Core Web API, EF Core, PostgreSQL/Npgsql, System.Threading.Channels, SignalR, Microsoft.SemanticKernel, Microsoft.Playwright .NET, React, TypeScript, Vite, TanStack Query, React Router, Tailwind CSS, xUnit, WebApplicationFactory, Vitest.

---

## Scope Check

The design covers backend, frontend, orchestration, evidence management, and PDF export. These are tightly coupled for the first runnable product, so this plan builds a single MVP vertical slice instead of splitting into independent subsystem plans. Real paid/free market-data providers, production web search, and live model API calls are not hard-coded in this plan; the plan creates interfaces, settings, fake providers, and tests so real providers can be added safely after the core workflow is stable.

## File Structure

Create this structure:

```text
StockAgent.NET/
  StockAgent.sln
  Directory.Build.props
  .editorconfig
  docker-compose.yml
  src/
    StockAgent.Api/
      Program.cs
      StockAgent.Api.csproj
      appsettings.Development.json
      Domain/
        AppSetting.cs
        DocumentChunk.cs
        DocumentSource.cs
        EvidenceCard.cs
        Market.cs
        ModelInvocation.cs
        PdfExport.cs
        ResearchReport.cs
        ResearchStage.cs
        ResearchStep.cs
        ResearchTask.cs
        ResearchTaskStatus.cs
        StepStatus.cs
      Features/
        Evidence/
          EvidenceEndpoints.cs
        Health/
          DataSourceHealthEndpoints.cs
        Pdf/
          PdfEndpoints.cs
        Reports/
          ReportEndpoints.cs
        ResearchTasks/
          ResearchTaskContracts.cs
          ResearchTaskEndpoints.cs
          TickerNormalizer.cs
        Settings/
          SettingsEndpoints.cs
      Infrastructure/
        Ai/
          AiAnalysisResult.cs
          IResearchAnalysisService.cs
          SemanticKernelResearchAnalysisService.cs
        DataSources/
          FakeMarketDataProvider.cs
          FakeWebResearchProvider.cs
          IMarketDataProvider.cs
          IWebResearchProvider.cs
          MarketDataSnapshot.cs
          WebEvidenceDocument.cs
        Documents/
          ContextBudgetManager.cs
          DocumentChunker.cs
          EvidenceCardFactory.cs
          EvidenceRetrievalService.cs
        Pdf/
          IPdfExportService.cs
          PlaywrightPdfExportService.cs
        Persistence/
          StockAgentDbContext.cs
        Queueing/
          IResearchTaskQueue.cs
          ResearchTaskQueue.cs
          ResearchWorker.cs
        Reports/
          ReportGenerator.cs
          ReportScore.cs
        Research/
          ResearchOrchestrator.cs
          ResearchPipelineOptions.cs
        Realtime/
          ResearchProgressHub.cs
      wwwroot/
        pdf/
    StockAgent.Web/
      package.json
      index.html
      src/
        App.tsx
        api/
          researchApi.ts
        components/
          EvidenceDrawer.tsx
          ReportViewer.tsx
          ResearchWorkbench.tsx
          SettingsPage.tsx
          TaskTimeline.tsx
        main.tsx
        models.ts
        styles.css
  tests/
    StockAgent.Api.Tests/
      StockAgent.Api.Tests.csproj
      ContextBudgetManagerTests.cs
      DocumentChunkerTests.cs
      ResearchTaskApiTests.cs
      ResearchTaskQueueTests.cs
      TickerNormalizerTests.cs
    StockAgent.Web.Tests/
      package.json
      src/
        ResearchWorkbench.test.tsx
```

## Task 1: Repository, Solution, And Quality Settings

**Files:**
- Create: `StockAgent.sln`
- Create: `Directory.Build.props`
- Create: `.editorconfig`
- Create: `src/StockAgent.Api/StockAgent.Api.csproj`
- Create: `tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj`

- [ ] **Step 1: Initialize git repository**

Run:

```powershell
git init
```

Expected: `Initialized empty Git repository` or `Reinitialized existing Git repository`.

- [ ] **Step 2: Scaffold .NET solution and projects**

Run:

```powershell
dotnet new sln -n StockAgent
dotnet new webapi -n StockAgent.Api -o src/StockAgent.Api --framework net10.0
dotnet new xunit -n StockAgent.Api.Tests -o tests/StockAgent.Api.Tests --framework net10.0
dotnet sln StockAgent.sln add src/StockAgent.Api/StockAgent.Api.csproj
dotnet sln StockAgent.sln add tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj
dotnet add tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj reference src/StockAgent.Api/StockAgent.Api.csproj
```

Expected: each command reports successful creation or project/reference addition.

- [ ] **Step 3: Add backend packages**

Run:

```powershell
dotnet add src/StockAgent.Api/StockAgent.Api.csproj package Microsoft.EntityFrameworkCore
dotnet add src/StockAgent.Api/StockAgent.Api.csproj package Microsoft.EntityFrameworkCore.Design
dotnet add src/StockAgent.Api/StockAgent.Api.csproj package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/StockAgent.Api/StockAgent.Api.csproj package Microsoft.SemanticKernel
dotnet add src/StockAgent.Api/StockAgent.Api.csproj package Microsoft.Playwright
dotnet add tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj package FluentAssertions
dotnet add tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj package Microsoft.EntityFrameworkCore.InMemory
```

Expected: package references are added.

- [ ] **Step 4: Create shared build settings**

Create `Directory.Build.props`:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>
</Project>
```

- [ ] **Step 5: Create comment and style diagnostics**

Create `.editorconfig`:

```ini
root = true

[*.cs]
dotnet_diagnostic.CS1591.severity = warning
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
dotnet_style_qualification_for_event = false:suggestion
csharp_style_namespace_declarations = file_scoped:suggestion

[*.{ts,tsx}]
charset = utf-8
indent_style = space
indent_size = 2
end_of_line = crlf
insert_final_newline = true
```

- [ ] **Step 6: Build solution**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit scaffold**

Run:

```powershell
git add StockAgent.sln Directory.Build.props .editorconfig src tests
git commit -m "chore: scaffold stock agent solution"
```

Expected: commit succeeds.

## Task 2: Domain Model And DbContext

**Files:**
- Create: `src/StockAgent.Api/Domain/*.cs`
- Create: `src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs`
- Modify: `src/StockAgent.Api/Program.cs`

- [ ] **Step 1: Create domain enums**

Create `src/StockAgent.Api/Domain/Market.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Identifies the supported stock markets for first-version research tasks.
/// </summary>
public enum Market
{
    /// <summary>A-share market, normally using Shanghai or Shenzhen exchange suffixes.</summary>
    AShare = 1,

    /// <summary>Hong Kong stock market, normally using the HK suffix.</summary>
    HongKong = 2
}
```

Create `src/StockAgent.Api/Domain/ResearchTaskStatus.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Durable lifecycle states for a stock research task.
/// </summary>
public enum ResearchTaskStatus
{
    /// <summary>The task has been saved and is waiting for a worker.</summary>
    Queued = 1,
    /// <summary>The worker has started executing the task.</summary>
    Running = 2,
    /// <summary>The task is gathering structured and public source data.</summary>
    CollectingData = 3,
    /// <summary>The task is parsing, chunking, and indexing source documents.</summary>
    IngestingDocuments = 4,
    /// <summary>The task is running bounded AI-assisted analysis.</summary>
    Analyzing = 5,
    /// <summary>The task is converting analysis outputs into the final report.</summary>
    GeneratingReport = 6,
    /// <summary>The report is ready for reading and PDF export.</summary>
    Ready = 7,
    /// <summary>The task is exporting a PDF copy of the report.</summary>
    ExportingPdf = 8,
    /// <summary>The research task and any requested PDF export completed successfully.</summary>
    Completed = 9,
    /// <summary>The task failed at a specific recoverable stage.</summary>
    Failed = 10,
    /// <summary>The task was cancelled before completion.</summary>
    Cancelled = 11
}
```

Create `src/StockAgent.Api/Domain/ResearchStage.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Ordered pipeline stages used by the research orchestrator.
/// </summary>
public enum ResearchStage
{
    /// <summary>Normalize and validate ticker input.</summary>
    NormalizeTicker = 1,
    /// <summary>Collect market profile, price, valuation, and financial snapshots.</summary>
    CollectStructuredData = 2,
    /// <summary>Collect public documents and web evidence.</summary>
    CollectPublicEvidence = 3,
    /// <summary>Parse, chunk, and index collected documents.</summary>
    IngestAndIndexDocuments = 4,
    /// <summary>Run Semantic Kernel-backed analysis over bounded evidence packs.</summary>
    AnalyzeWithSemanticKernel = 5,
    /// <summary>Create structured scoring and rating output.</summary>
    ScoreAndRate = 6,
    /// <summary>Generate the final Chinese Markdown and HTML report.</summary>
    GenerateReport = 7,
    /// <summary>Export the report to PDF when requested.</summary>
    ExportPdf = 8
}
```

Create `src/StockAgent.Api/Domain/StepStatus.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Execution state for an individual research pipeline step.
/// </summary>
public enum StepStatus
{
    /// <summary>The step has not started.</summary>
    Pending = 1,
    /// <summary>The step is currently running.</summary>
    Running = 2,
    /// <summary>The step completed successfully.</summary>
    Succeeded = 3,
    /// <summary>The step failed and can be inspected or retried.</summary>
    Failed = 4,
    /// <summary>The step was skipped because the task was cancelled or no longer needed.</summary>
    Skipped = 5
}
```

- [ ] **Step 2: Create core entity files**

Create `src/StockAgent.Api/Domain/ResearchTask.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Root entity for a user-submitted stock research workflow.
/// </summary>
public sealed class ResearchTask
{
    /// <summary>Unique task identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Normalized ticker, such as 600519.SH or 00700.HK.</summary>
    public string Ticker { get; set; } = string.Empty;

    /// <summary>Supported market for the ticker.</summary>
    public Market Market { get; set; }

    /// <summary>Company name when known from data providers.</summary>
    public string? CompanyName { get; set; }

    /// <summary>Durable lifecycle state.</summary>
    public ResearchTaskStatus Status { get; set; } = ResearchTaskStatus.Queued;

    /// <summary>Current pipeline stage, if the task has started.</summary>
    public ResearchStage? CurrentStage { get; set; }

    /// <summary>Approximate task progress from 0 to 100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Latest task-level failure message.</summary>
    public string? ErrorMessage { get; set; }

    /// <summary>Report language. The MVP defaults to zh-CN.</summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>Creation timestamp in UTC.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Last update timestamp in UTC.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Step records for this task.</summary>
    public List<ResearchStep> Steps { get; set; } = [];
}
```

Create `src/StockAgent.Api/Domain/ResearchStep.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>
/// Durable audit record for one stage of a research task.
/// </summary>
public sealed class ResearchStep
{
    /// <summary>Unique step identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }

    /// <summary>Parent research task.</summary>
    public ResearchTask? ResearchTask { get; set; }

    /// <summary>Pipeline stage represented by this step.</summary>
    public ResearchStage StepName { get; set; }

    /// <summary>Execution status for this step.</summary>
    public StepStatus Status { get; set; } = StepStatus.Pending;

    /// <summary>Number of retry attempts for this step.</summary>
    public int RetryCount { get; set; }

    /// <summary>UTC timestamp when execution started.</summary>
    public DateTimeOffset? StartedAt { get; set; }

    /// <summary>UTC timestamp when execution ended.</summary>
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>Short summary of the step input.</summary>
    public string? InputSummary { get; set; }

    /// <summary>Short summary of the step output.</summary>
    public string? OutputSummary { get; set; }

    /// <summary>Failure details safe to display in the UI.</summary>
    public string? ErrorMessage { get; set; }
}
```

- [ ] **Step 3: Create evidence, report, and settings entities**

Create the remaining domain files with the exact public properties from the design:

```csharp
// File: src/StockAgent.Api/Domain/DocumentSource.cs
namespace StockAgent.Api.Domain;

/// <summary>Original source document or web page collected for a research task.</summary>
public sealed class DocumentSource
{
    /// <summary>Unique source identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source URL when available.</summary>
    public string Url { get; set; } = string.Empty;
    /// <summary>Human-readable source title.</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>Source type such as filing, report, news, or company-site.</summary>
    public string SourceType { get; set; } = string.Empty;
    /// <summary>Publisher or host name.</summary>
    public string? Publisher { get; set; }
    /// <summary>Original publication timestamp when known.</summary>
    public DateTimeOffset? PublishedAt { get; set; }
    /// <summary>UTC timestamp when the system retrieved the source.</summary>
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Stable hash used for de-duplication.</summary>
    public string ContentHash { get; set; } = string.Empty;
    /// <summary>Path to stored raw content.</summary>
    public string? RawContentPath { get; set; }
    /// <summary>Path to parsed text content.</summary>
    public string? ParsedContentPath { get; set; }
}

// File: src/StockAgent.Api/Domain/DocumentChunk.cs
namespace StockAgent.Api.Domain;

/// <summary>Bounded text block derived from a source document.</summary>
public sealed class DocumentChunk
{
    /// <summary>Unique chunk identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent source document identifier.</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Zero-based chunk index within the source.</summary>
    public int ChunkIndex { get; set; }
    /// <summary>Page number for PDF-derived chunks when available.</summary>
    public int? PageNumber { get; set; }
    /// <summary>Section heading near the chunk when available.</summary>
    public string? SectionTitle { get; set; }
    /// <summary>Chunk text sent through retrieval and summarization.</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Rough token estimate for context budgeting.</summary>
    public int TokenEstimate { get; set; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

// File: src/StockAgent.Api/Domain/EvidenceCard.cs
namespace StockAgent.Api.Domain;

/// <summary>Compressed, citation-ready evidence extracted from one document chunk.</summary>
public sealed class EvidenceCard
{
    /// <summary>Unique evidence identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Source document identifier.</summary>
    public Guid DocumentSourceId { get; set; }
    /// <summary>Chunk identifier that produced this card.</summary>
    public Guid DocumentChunkId { get; set; }
    /// <summary>Short factual claim extracted from the chunk.</summary>
    public string Claim { get; set; } = string.Empty;
    /// <summary>Metric or topic associated with the claim.</summary>
    public string? Metric { get; set; }
    /// <summary>Short quote or paraphrased snippet for display.</summary>
    public string Snippet { get; set; } = string.Empty;
    /// <summary>Confidence score from 0 to 1.</summary>
    public decimal Confidence { get; set; }
    /// <summary>Relevance score from 0 to 1 for retrieval ranking.</summary>
    public decimal Relevance { get; set; }
    /// <summary>Source publication date when available.</summary>
    public DateTimeOffset? SourceDate { get; set; }
    /// <summary>Report section where the card is most useful.</summary>
    public string ReportSection { get; set; } = string.Empty;
}
```

Create `src/StockAgent.Api/Domain/ResearchReport.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>Generated stock research report persisted for reading and PDF export.</summary>
public sealed class ResearchReport
{
    /// <summary>Unique report identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Report language such as zh-CN.</summary>
    public string Language { get; set; } = "zh-CN";
    /// <summary>Markdown report body.</summary>
    public string Markdown { get; set; } = string.Empty;
    /// <summary>HTML report body rendered from Markdown.</summary>
    public string Html { get; set; } = string.Empty;
    /// <summary>Serialized structured rating JSON.</summary>
    public string RatingJson { get; set; } = "{}";
    /// <summary>Data cutoff timestamp for the research report.</summary>
    public DateTimeOffset DataCutoffAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>Model provider used to generate the report.</summary>
    public string ModelProvider { get; set; } = "deterministic";
    /// <summary>Model name used to generate the report.</summary>
    public string ModelName { get; set; } = "fake-analysis-v1";
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

Create `src/StockAgent.Api/Domain/PdfExport.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>PDF export audit record for a research report.</summary>
public sealed class PdfExport
{
    /// <summary>Unique PDF export identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Export status such as Queued, Running, Completed, or Failed.</summary>
    public string Status { get; set; } = "Queued";
    /// <summary>Server file path for the generated PDF.</summary>
    public string? FilePath { get; set; }
    /// <summary>UTC timestamp when export was requested.</summary>
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
    /// <summary>UTC timestamp when export completed.</summary>
    public DateTimeOffset? CompletedAt { get; set; }
    /// <summary>Failure message safe to display in the UI.</summary>
    public string? ErrorMessage { get; set; }
}
```

Create `src/StockAgent.Api/Domain/ModelInvocation.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>Audit record for one model or deterministic analysis invocation.</summary>
public sealed class ModelInvocation
{
    /// <summary>Unique invocation identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Parent research task identifier.</summary>
    public Guid ResearchTaskId { get; set; }
    /// <summary>Pipeline stage that triggered the invocation.</summary>
    public string StepName { get; set; } = string.Empty;
    /// <summary>Provider name such as OpenAI, Compatible, or Deterministic.</summary>
    public string Provider { get; set; } = string.Empty;
    /// <summary>Model name or deterministic analyzer name.</summary>
    public string ModelName { get; set; } = string.Empty;
    /// <summary>Prompt token count when available.</summary>
    public int? PromptTokens { get; set; }
    /// <summary>Completion token count when available.</summary>
    public int? CompletionTokens { get; set; }
    /// <summary>Invocation duration in milliseconds.</summary>
    public long DurationMs { get; set; }
    /// <summary>Invocation status such as Succeeded or Failed.</summary>
    public string Status { get; set; } = "Succeeded";
    /// <summary>Failure message safe to persist.</summary>
    public string? ErrorMessage { get; set; }
    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

Create `src/StockAgent.Api/Domain/AppSetting.cs`:

```csharp
namespace StockAgent.Api.Domain;

/// <summary>JSON-backed application setting for provider and research configuration.</summary>
public sealed class AppSetting
{
    /// <summary>Unique setting identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Stable setting key.</summary>
    public string SettingKey { get; set; } = string.Empty;
    /// <summary>Serialized JSON setting value.</summary>
    public string SettingValueJson { get; set; } = "{}";
    /// <summary>UTC timestamp when the setting was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

- [ ] **Step 4: Create EF Core DbContext**

Create `src/StockAgent.Api/Infrastructure/Persistence/StockAgentDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for research tasks, evidence, reports, and provider audit records.
/// </summary>
public sealed class StockAgentDbContext(DbContextOptions<StockAgentDbContext> options) : DbContext(options)
{
    /// <summary>Research task roots.</summary>
    public DbSet<ResearchTask> ResearchTasks => Set<ResearchTask>();
    /// <summary>Research stage audit records.</summary>
    public DbSet<ResearchStep> ResearchSteps => Set<ResearchStep>();
    /// <summary>Collected source documents.</summary>
    public DbSet<DocumentSource> DocumentSources => Set<DocumentSource>();
    /// <summary>Parsed document chunks.</summary>
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    /// <summary>Compressed evidence cards.</summary>
    public DbSet<EvidenceCard> EvidenceCards => Set<EvidenceCard>();
    /// <summary>Generated reports.</summary>
    public DbSet<ResearchReport> ResearchReports => Set<ResearchReport>();
    /// <summary>PDF export records.</summary>
    public DbSet<PdfExport> PdfExports => Set<PdfExport>();
    /// <summary>Model invocation audit records.</summary>
    public DbSet<ModelInvocation> ModelInvocations => Set<ModelInvocation>();
    /// <summary>Application settings stored as JSON strings.</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ResearchTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Ticker).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(256);
            entity.Property(x => x.Language).HasMaxLength(16).IsRequired();
            entity.HasMany(x => x.Steps).WithOne(x => x.ResearchTask).HasForeignKey(x => x.ResearchTaskId);
        });

        modelBuilder.Entity<ResearchStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InputSummary).HasMaxLength(2000);
            entity.Property(x => x.OutputSummary).HasMaxLength(2000);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
        });

        modelBuilder.Entity<DocumentSource>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Url).HasMaxLength(2048).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(512).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ContentHash).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => new { x.ResearchTaskId, x.ContentHash }).IsUnique();
        });

        modelBuilder.Entity<DocumentChunk>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Text).IsRequired();
            entity.HasIndex(x => new { x.DocumentSourceId, x.ChunkIndex }).IsUnique();
        });

        modelBuilder.Entity<EvidenceCard>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Claim).HasMaxLength(1000).IsRequired();
            entity.Property(x => x.Snippet).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ReportSection).HasMaxLength(128).IsRequired();
        });
    }
}
```

- [ ] **Step 5: Register DbContext in Program.cs**

Modify `src/StockAgent.Api/Program.cs` so it includes:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<StockAgentDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("StockAgent");
    options.UseNpgsql(connectionString);
});
```

Keep the generated Swagger setup.

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds with XML comment warnings only for files not yet documented.

- [ ] **Step 7: Commit domain model**

Run:

```powershell
git add src/StockAgent.Api tests/StockAgent.Api.Tests
git commit -m "feat: add research domain model"
```

Expected: commit succeeds.

## Task 3: Ticker Normalization

**Files:**
- Create: `src/StockAgent.Api/Features/ResearchTasks/TickerNormalizer.cs`
- Test: `tests/StockAgent.Api.Tests/TickerNormalizerTests.cs`

- [ ] **Step 1: Write failing ticker tests**

Create `tests/StockAgent.Api.Tests/TickerNormalizerTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies market-specific ticker normalization for first-version A-share and Hong Kong symbols.
/// </summary>
public sealed class TickerNormalizerTests
{
    /// <summary>
    /// A 6-digit Shanghai symbol is normalized with the SH suffix.
    /// </summary>
    [Fact]
    public void Normalize_AddsShanghaiSuffix_ForSixHundredPrefix()
    {
        var result = TickerNormalizer.Normalize("600519", null);

        result.Ticker.Should().Be("600519.SH");
        result.Market.Should().Be(Market.AShare);
    }

    /// <summary>
    /// A Hong Kong numeric code is normalized to five digits with HK suffix.
    /// </summary>
    [Fact]
    public void Normalize_PadsHongKongTicker_ToFiveDigits()
    {
        var result = TickerNormalizer.Normalize("700", Market.HongKong);

        result.Ticker.Should().Be("00700.HK");
        result.Market.Should().Be(Market.HongKong);
    }

    /// <summary>
    /// Unsupported ticker input fails before creating a task.
    /// </summary>
    [Fact]
    public void Normalize_RejectsUnsupportedTicker()
    {
        var act = () => TickerNormalizer.Normalize("abc", null);

        act.Should().Throw<ArgumentException>().WithMessage("*Unsupported ticker*");
    }
}
```

- [ ] **Step 2: Run tests and confirm failure**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter TickerNormalizerTests
```

Expected: compile fails because `TickerNormalizer` does not exist.

- [ ] **Step 3: Implement normalizer**

Create `src/StockAgent.Api/Features/ResearchTasks/TickerNormalizer.cs`:

```csharp
using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Normalizes user-entered A-share and Hong Kong stock tickers into canonical suffix format.
/// </summary>
public static class TickerNormalizer
{
    /// <summary>
    /// Normalizes a ticker and infers the market when possible.
    /// </summary>
    /// <param name="input">User-entered ticker such as 600519, 600519.SH, 700, or 00700.HK.</param>
    /// <param name="marketHint">Optional market selected by the user.</param>
    /// <returns>Canonical ticker and market.</returns>
    /// <exception cref="ArgumentException">Thrown when the ticker is unsupported or ambiguous.</exception>
    public static NormalizedTicker Normalize(string input, Market? marketHint)
    {
        var trimmed = input.Trim().ToUpperInvariant();

        if (trimmed.EndsWith(".SH", StringComparison.Ordinal) || trimmed.EndsWith(".SZ", StringComparison.Ordinal))
        {
            return new NormalizedTicker(trimmed, Market.AShare);
        }

        if (trimmed.EndsWith(".HK", StringComparison.Ordinal))
        {
            var code = trimmed[..^3].PadLeft(5, '0');
            return new NormalizedTicker($"{code}.HK", Market.HongKong);
        }

        if (trimmed.Length == 6 && trimmed.All(char.IsDigit))
        {
            var suffix = trimmed.StartsWith('6') ? "SH" : "SZ";
            return new NormalizedTicker($"{trimmed}.{suffix}", Market.AShare);
        }

        if (marketHint == Market.HongKong && trimmed.Length <= 5 && trimmed.All(char.IsDigit))
        {
            return new NormalizedTicker($"{trimmed.PadLeft(5, '0')}.HK", Market.HongKong);
        }

        throw new ArgumentException($"Unsupported ticker input: {input}", nameof(input));
    }
}

/// <summary>
/// Canonical ticker value returned by the ticker normalizer.
/// </summary>
/// <param name="Ticker">Normalized ticker symbol.</param>
/// <param name="Market">Resolved market.</param>
public sealed record NormalizedTicker(string Ticker, Market Market);
```

- [ ] **Step 4: Run ticker tests**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter TickerNormalizerTests
```

Expected: tests pass.

- [ ] **Step 5: Commit normalizer**

Run:

```powershell
git add src/StockAgent.Api/Features/ResearchTasks/TickerNormalizer.cs tests/StockAgent.Api.Tests/TickerNormalizerTests.cs
git commit -m "feat: normalize stock tickers"
```

Expected: commit succeeds.

## Task 4: Research Task API And Queue

**Files:**
- Create: `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskContracts.cs`
- Create: `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskEndpoints.cs`
- Create: `src/StockAgent.Api/Infrastructure/Queueing/IResearchTaskQueue.cs`
- Create: `src/StockAgent.Api/Infrastructure/Queueing/ResearchTaskQueue.cs`
- Modify: `src/StockAgent.Api/Program.cs`
- Test: `tests/StockAgent.Api.Tests/ResearchTaskQueueTests.cs`
- Test: `tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs`

- [ ] **Step 1: Write queue tests**

Create `tests/StockAgent.Api.Tests/ResearchTaskQueueTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Infrastructure.Queueing;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies that research task IDs can be queued and consumed in FIFO order.
/// </summary>
public sealed class ResearchTaskQueueTests
{
    /// <summary>
    /// A queued task ID is read by the worker consumer.
    /// </summary>
    [Fact]
    public async Task QueueAsync_ThenDequeueAsync_ReturnsTaskId()
    {
        var queue = new ResearchTaskQueue();
        var taskId = Guid.NewGuid();

        await queue.QueueAsync(taskId, CancellationToken.None);
        var result = await queue.DequeueAsync(CancellationToken.None);

        result.Should().Be(taskId);
    }
}
```

- [ ] **Step 2: Run queue test and confirm failure**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter ResearchTaskQueueTests
```

Expected: compile fails because queue types do not exist.

- [ ] **Step 3: Implement queue**

Create `src/StockAgent.Api/Infrastructure/Queueing/IResearchTaskQueue.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// In-process queue for research task identifiers awaiting background execution.
/// </summary>
public interface IResearchTaskQueue
{
    /// <summary>Queues a task identifier for worker execution.</summary>
    Task QueueAsync(Guid researchTaskId, CancellationToken cancellationToken);

    /// <summary>Reads the next queued task identifier.</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken);
}
```

Create `src/StockAgent.Api/Infrastructure/Queueing/ResearchTaskQueue.cs`:

```csharp
using System.Threading.Channels;

namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// Channel-backed in-memory queue used by the first-version modular monolith.
/// </summary>
public sealed class ResearchTaskQueue : IResearchTaskQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

    /// <inheritdoc />
    public async Task QueueAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(researchTaskId, cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<Guid> DequeueAsync(CancellationToken cancellationToken)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Create task contracts and endpoints**

Create `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskContracts.cs`:

```csharp
using StockAgent.Api.Domain;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>Request body for creating a stock research task.</summary>
public sealed record CreateResearchTaskRequest(string Ticker, Market? Market, string? Language);

/// <summary>Response returned after a research task is created.</summary>
public sealed record ResearchTaskResponse(Guid Id, string Ticker, Market Market, ResearchTaskStatus Status, int ProgressPercent, string Language);
```

Create `src/StockAgent.Api/Features/ResearchTasks/ResearchTaskEndpoints.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Persistence;
using StockAgent.Api.Infrastructure.Queueing;

namespace StockAgent.Api.Features.ResearchTasks;

/// <summary>
/// Minimal API endpoints for creating and reading research tasks.
/// </summary>
public static class ResearchTaskEndpoints
{
    /// <summary>Maps research task endpoints.</summary>
    public static IEndpointRouteBuilder MapResearchTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/research-tasks").WithTags("Research Tasks");

        group.MapPost("/", async (
            CreateResearchTaskRequest request,
            StockAgentDbContext db,
            IResearchTaskQueue queue,
            CancellationToken cancellationToken) =>
        {
            var normalized = TickerNormalizer.Normalize(request.Ticker, request.Market);
            var task = new ResearchTask
            {
                Ticker = normalized.Ticker,
                Market = normalized.Market,
                Language = string.IsNullOrWhiteSpace(request.Language) ? "zh-CN" : request.Language.Trim()
            };

            db.ResearchTasks.Add(task);
            await db.SaveChangesAsync(cancellationToken);
            await queue.QueueAsync(task.Id, cancellationToken);

            return Results.Created($"/api/research-tasks/{task.Id}", ToResponse(task));
        });

        group.MapGet("/", async (StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var tasks = await db.ResearchTasks
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new ResearchTaskResponse(x.Id, x.Ticker, x.Market, x.Status, x.ProgressPercent, x.Language))
                .ToListAsync(cancellationToken);

            return Results.Ok(tasks);
        });

        group.MapGet("/{id:guid}", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var task = await db.ResearchTasks.FindAsync([id], cancellationToken);
            return task is null ? Results.NotFound() : Results.Ok(ToResponse(task));
        });

        return app;
    }

    private static ResearchTaskResponse ToResponse(ResearchTask task)
    {
        return new ResearchTaskResponse(task.Id, task.Ticker, task.Market, task.Status, task.ProgressPercent, task.Language);
    }
}
```

- [ ] **Step 5: Register queue and endpoints**

Modify `Program.cs`:

```csharp
using System.Text.Json.Serialization;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Queueing;

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddSingleton<IResearchTaskQueue, ResearchTaskQueue>();

var app = builder.Build();

app.MapResearchTaskEndpoints();
```

Keep Swagger and HTTPS redirection from the template.

- [ ] **Step 6: Expose Program for integration tests**

Add this line at the end of `Program.cs`:

```csharp
/// <summary>Marker type used by WebApplicationFactory integration tests.</summary>
public partial class Program;
```

- [ ] **Step 7: Add API integration test**

Create `tests/StockAgent.Api.Tests/ResearchTaskApiTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StockAgent.Api.Domain;
using StockAgent.Api.Features.ResearchTasks;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the research task API contract used by the React workbench.
/// </summary>
public sealed class ResearchTaskApiTests
{
    /// <summary>
    /// Creating a Hong Kong research task returns a string-enum response with normalized ticker.
    /// </summary>
    [Fact]
    public async Task CreateResearchTask_ReturnsCreatedTask()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<StockAgentDbContext>>();
                    services.AddDbContext<StockAgentDbContext>(options =>
                        options.UseInMemoryDatabase($"stockagent-{Guid.NewGuid()}"));
                });
            });

        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/research-tasks",
            new CreateResearchTaskRequest("700", Market.HongKong, "zh-CN"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<ResearchTaskResponse>();
        body.Should().NotBeNull();
        body!.Ticker.Should().Be("00700.HK");
        body.Status.Should().Be(ResearchTaskStatus.Queued);
    }
}
```

- [ ] **Step 8: Run queue and API tests**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter "ResearchTaskQueueTests|ResearchTaskApiTests"
```

Expected: tests pass.

- [ ] **Step 9: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 10: Commit task API and queue**

Run:

```powershell
git add src/StockAgent.Api tests/StockAgent.Api.Tests
git commit -m "feat: add research task queue and API"
```

Expected: commit succeeds.

## Task 5: Orchestrator With Deterministic Fake Providers

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`
- Create: `src/StockAgent.Api/Infrastructure/Research/ResearchPipelineOptions.cs`
- Create: `src/StockAgent.Api/Infrastructure/Queueing/ResearchWorker.cs`
- Create: `src/StockAgent.Api/Infrastructure/DataSources/*.cs`
- Modify: `src/StockAgent.Api/Program.cs`

- [ ] **Step 1: Create provider contracts**

Create `src/StockAgent.Api/Infrastructure/DataSources/MarketDataSnapshot.cs`:

```csharp
using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Structured market and financial snapshot used by the research pipeline.
/// </summary>
public sealed record MarketDataSnapshot(
    string Ticker,
    Market Market,
    string CompanyName,
    decimal LastPrice,
    decimal MarketCap,
    decimal PeRatio,
    decimal RevenueGrowthPercent,
    decimal NetMarginPercent);
```

Create `src/StockAgent.Api/Infrastructure/DataSources/WebEvidenceDocument.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Public source document collected from a web/search provider.
/// </summary>
public sealed record WebEvidenceDocument(
    string Url,
    string Title,
    string SourceType,
    DateTimeOffset? PublishedAt,
    string Text);
```

Create `IMarketDataProvider.cs` and `IWebResearchProvider.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>Fetches structured market and financial data for a normalized ticker.</summary>
public interface IMarketDataProvider
{
    /// <summary>Gets a deterministic structured snapshot for the requested ticker.</summary>
    Task<MarketDataSnapshot> GetSnapshotAsync(string ticker, CancellationToken cancellationToken);
}

/// <summary>Finds public documents that can support a research report.</summary>
public interface IWebResearchProvider
{
    /// <summary>Returns public evidence documents for a normalized ticker and company name.</summary>
    Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(string ticker, string companyName, CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Create fake providers**

Create `FakeMarketDataProvider.cs` and `FakeWebResearchProvider.cs`:

```csharp
using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.DataSources;

/// <summary>
/// Deterministic market data provider used to keep the MVP workflow testable before real provider selection.
/// </summary>
public sealed class FakeMarketDataProvider : IMarketDataProvider
{
    /// <inheritdoc />
    public Task<MarketDataSnapshot> GetSnapshotAsync(string ticker, CancellationToken cancellationToken)
    {
        var market = ticker.EndsWith(".HK", StringComparison.OrdinalIgnoreCase) ? Market.HongKong : Market.AShare;
        var companyName = market == Market.HongKong ? "腾讯控股" : "示例公司";
        var snapshot = new MarketDataSnapshot(ticker, market, companyName, 320.50m, 3_000_000_000_000m, 18.4m, 8.2m, 24.5m);
        return Task.FromResult(snapshot);
    }
}

/// <summary>
/// Deterministic web research provider that returns representative public evidence documents.
/// </summary>
public sealed class FakeWebResearchProvider : IWebResearchProvider
{
    /// <inheritdoc />
    public Task<IReadOnlyList<WebEvidenceDocument>> SearchAsync(string ticker, string companyName, CancellationToken cancellationToken)
    {
        IReadOnlyList<WebEvidenceDocument> documents =
        [
            new WebEvidenceDocument(
                "https://example.local/annual-report",
                $"{companyName} 年报摘要",
                "annual-report",
                DateTimeOffset.UtcNow.AddMonths(-3),
                $"{companyName} 收入保持增长，经营利润率稳定，现金流表现稳健。管理层提示宏观需求和监管变化是主要风险。"),
            new WebEvidenceDocument(
                "https://example.local/news",
                $"{companyName} 业务进展",
                "news",
                DateTimeOffset.UtcNow.AddDays(-14),
                $"{companyName} 核心业务保持用户规模优势，新业务投入仍影响短期利润率。")
        ];

        return Task.FromResult(documents);
    }
}
```

- [ ] **Step 3: Create orchestrator skeleton**

Create `src/StockAgent.Api/Infrastructure/Research/ResearchOrchestrator.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Infrastructure.Research;

/// <summary>
/// Durable manager for the stock research pipeline. It owns stage transitions and delegates specialized work to providers.
/// </summary>
public sealed class ResearchOrchestrator(
    StockAgentDbContext db,
    IMarketDataProvider marketDataProvider,
    IWebResearchProvider webResearchProvider,
    ILogger<ResearchOrchestrator> logger)
{
    /// <summary>
    /// Executes the first runnable research pipeline for one queued task.
    /// </summary>
    public async Task RunAsync(Guid researchTaskId, CancellationToken cancellationToken)
    {
        var task = await db.ResearchTasks.FirstAsync(x => x.Id == researchTaskId, cancellationToken);
        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectStructuredData, 10, cancellationToken);

        var snapshot = await marketDataProvider.GetSnapshotAsync(task.Ticker, cancellationToken);
        task.CompanyName = snapshot.CompanyName;

        await SetStatusAsync(task, ResearchTaskStatus.CollectingData, ResearchStage.CollectPublicEvidence, 30, cancellationToken);
        var documents = await webResearchProvider.SearchAsync(task.Ticker, snapshot.CompanyName, cancellationToken);

        logger.LogInformation("Collected {DocumentCount} fake evidence documents for {Ticker}", documents.Count, task.Ticker);

        await SetStatusAsync(task, ResearchTaskStatus.Ready, ResearchStage.GenerateReport, 100, cancellationToken);
    }

    private async Task SetStatusAsync(
        ResearchTask task,
        ResearchTaskStatus status,
        ResearchStage stage,
        int progress,
        CancellationToken cancellationToken)
    {
        task.Status = status;
        task.CurrentStage = stage;
        task.ProgressPercent = progress;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        db.ResearchSteps.Add(new ResearchStep
        {
            ResearchTaskId = task.Id,
            StepName = stage,
            Status = StepStatus.Succeeded,
            StartedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
            OutputSummary = $"Stage {stage} completed."
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Create worker**

Create `src/StockAgent.Api/Infrastructure/Queueing/ResearchWorker.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using StockAgent.Api.Infrastructure.Research;

namespace StockAgent.Api.Infrastructure.Queueing;

/// <summary>
/// Background worker that consumes queued research task IDs and runs the durable orchestrator.
/// </summary>
public sealed class ResearchWorker(
    IResearchTaskQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<ResearchWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var taskId = await queue.DequeueAsync(stoppingToken);

            using var scope = scopeFactory.CreateScope();
            var orchestrator = scope.ServiceProvider.GetRequiredService<ResearchOrchestrator>();

            try
            {
                await orchestrator.RunAsync(taskId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Research task {TaskId} failed in background worker.", taskId);
            }
        }
    }
}
```

- [ ] **Step 5: Register providers and worker**

Modify `Program.cs`:

```csharp
using StockAgent.Api.Infrastructure.DataSources;
using StockAgent.Api.Infrastructure.Research;

builder.Services.AddScoped<IMarketDataProvider, FakeMarketDataProvider>();
builder.Services.AddScoped<IWebResearchProvider, FakeWebResearchProvider>();
builder.Services.AddScoped<ResearchOrchestrator>();
builder.Services.AddHostedService<ResearchWorker>();
```

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit orchestrator skeleton**

Run:

```powershell
git add src/StockAgent.Api
git commit -m "feat: add research orchestrator skeleton"
```

Expected: commit succeeds.

## Task 6: Document Chunking And Context Budget

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Documents/DocumentChunker.cs`
- Create: `src/StockAgent.Api/Infrastructure/Documents/ContextBudgetManager.cs`
- Test: `tests/StockAgent.Api.Tests/DocumentChunkerTests.cs`
- Test: `tests/StockAgent.Api.Tests/ContextBudgetManagerTests.cs`

- [ ] **Step 1: Write chunker tests**

Create `tests/StockAgent.Api.Tests/DocumentChunkerTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Infrastructure.Documents;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies bounded text chunking for long public documents.
/// </summary>
public sealed class DocumentChunkerTests
{
    /// <summary>
    /// Long text is split into chunks that respect the configured character budget.
    /// </summary>
    [Fact]
    public void Chunk_SplitsLongText_ByCharacterBudget()
    {
        var chunker = new DocumentChunker();
        var text = string.Join("", Enumerable.Repeat("收入增长稳定。", 300));

        var chunks = chunker.Chunk(text, maxCharacters: 120).ToList();

        chunks.Should().NotBeEmpty();
        chunks.Should().OnlyContain(x => x.Text.Length <= 120);
    }
}
```

- [ ] **Step 2: Write context budget tests**

Create `tests/StockAgent.Api.Tests/ContextBudgetManagerTests.cs`:

```csharp
using FluentAssertions;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Documents;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies that evidence packs are capped before model calls.
/// </summary>
public sealed class ContextBudgetManagerTests
{
    /// <summary>
    /// The highest relevance evidence cards are kept within the requested limit.
    /// </summary>
    [Fact]
    public void SelectEvidence_KeepsHighestRelevanceCards()
    {
        var manager = new ContextBudgetManager();
        var cards = Enumerable.Range(1, 10)
            .Select(i => new EvidenceCard { Claim = $"claim-{i}", Relevance = i / 10m, Snippet = "snippet", ReportSection = "Risk" })
            .ToList();

        var selected = manager.SelectEvidence(cards, maxCards: 3);

        selected.Select(x => x.Claim).Should().Equal("claim-10", "claim-9", "claim-8");
    }
}
```

- [ ] **Step 3: Run tests and confirm failure**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter "DocumentChunkerTests|ContextBudgetManagerTests"
```

Expected: compile fails because document services do not exist.

- [ ] **Step 4: Implement document services**

Create `src/StockAgent.Api/Infrastructure/Documents/DocumentChunker.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Splits long source text into bounded chunks so raw documents are never sent wholesale to a model.
/// </summary>
public sealed class DocumentChunker
{
    /// <summary>
    /// Splits text into character-bounded chunks.
    /// </summary>
    public IEnumerable<DocumentTextChunk> Chunk(string text, int maxCharacters)
    {
        if (maxCharacters <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharacters), "Chunk size must be positive.");
        }

        var normalized = text.Trim();
        for (var index = 0; index < normalized.Length; index += maxCharacters)
        {
            var length = Math.Min(maxCharacters, normalized.Length - index);
            yield return new DocumentTextChunk(index / maxCharacters, normalized.Substring(index, length), EstimateTokens(length));
        }
    }

    private static int EstimateTokens(int characterCount)
    {
        return Math.Max(1, characterCount / 2);
    }
}

/// <summary>
/// In-memory chunk produced before persistence as a DocumentChunk entity.
/// </summary>
public sealed record DocumentTextChunk(int Index, string Text, int TokenEstimate);
```

Create `src/StockAgent.Api/Infrastructure/Documents/ContextBudgetManager.cs`:

```csharp
using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Selects bounded evidence packs for model calls according to explicit context limits.
/// </summary>
public sealed class ContextBudgetManager
{
    /// <summary>
    /// Returns the most relevant evidence cards while respecting the maximum card count.
    /// </summary>
    public IReadOnlyList<EvidenceCard> SelectEvidence(IEnumerable<EvidenceCard> evidenceCards, int maxCards)
    {
        if (maxCards <= 0)
        {
            return [];
        }

        return evidenceCards
            .OrderByDescending(x => x.Relevance)
            .ThenByDescending(x => x.Confidence)
            .Take(maxCards)
            .ToList();
    }
}
```

- [ ] **Step 5: Run tests**

Run:

```powershell
dotnet test tests/StockAgent.Api.Tests/StockAgent.Api.Tests.csproj --filter "DocumentChunkerTests|ContextBudgetManagerTests"
```

Expected: tests pass.

- [ ] **Step 6: Commit context controls**

Run:

```powershell
git add src/StockAgent.Api/Infrastructure/Documents tests/StockAgent.Api.Tests
git commit -m "feat: add document chunking and context budget"
```

Expected: commit succeeds.

## Task 7: AI Analysis Boundary With Semantic Kernel-Ready Service

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs`
- Create: `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`
- Modify: `src/StockAgent.Api/Program.cs`

- [ ] **Step 1: Create AI analysis contract**

Create `src/StockAgent.Api/Infrastructure/Ai/AiAnalysisResult.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Structured AI analysis result used by scoring and report generation.
/// </summary>
public sealed record AiAnalysisResult(
    int OverallScore,
    string RiskLevel,
    string ValuationView,
    string Summary,
    IReadOnlyList<string> KeyAssumptions);
```

Create `src/StockAgent.Api/Infrastructure/Ai/IResearchAnalysisService.cs`:

```csharp
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// Boundary for Semantic Kernel-backed research analysis over bounded evidence packs.
/// </summary>
public interface IResearchAnalysisService
{
    /// <summary>
    /// Analyzes structured market data and selected evidence cards.
    /// </summary>
    Task<AiAnalysisResult> AnalyzeAsync(
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        CancellationToken cancellationToken);
}
```

- [ ] **Step 2: Implement deterministic Semantic Kernel-ready service**

Create `src/StockAgent.Api/Infrastructure/Ai/SemanticKernelResearchAnalysisService.cs`:

```csharp
using Microsoft.SemanticKernel;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Ai;

/// <summary>
/// First-version analysis service that owns the Semantic Kernel boundary while returning deterministic output for tests.
/// </summary>
public sealed class SemanticKernelResearchAnalysisService(Kernel kernel, ILogger<SemanticKernelResearchAnalysisService> logger)
    : IResearchAnalysisService
{
    /// <inheritdoc />
    public Task<AiAnalysisResult> AnalyzeAsync(
        MarketDataSnapshot marketData,
        IReadOnlyList<EvidenceCard> evidenceCards,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Semantic Kernel boundary invoked for {Ticker} with {EvidenceCount} evidence cards.", marketData.Ticker, evidenceCards.Count);

        var score = marketData.PeRatio < 25 && marketData.RevenueGrowthPercent > 0 ? 76 : 62;
        var result = new AiAnalysisResult(
            score,
            "中等",
            marketData.PeRatio < 20 ? "估值相对合理" : "估值需要结合增长验证",
            $"{marketData.CompanyName} 基本面保持稳定，证据数量为 {evidenceCards.Count} 条。",
            ["收入增长延续", "利润率保持稳定", "监管和宏观需求未显著恶化"]);

        return Task.FromResult(result);
    }
}
```

- [ ] **Step 3: Register Kernel and analysis service**

Modify `Program.cs`:

```csharp
using Microsoft.SemanticKernel;
using StockAgent.Api.Infrastructure.Ai;

builder.Services.AddSingleton(_ => Kernel.CreateBuilder().Build());
builder.Services.AddScoped<IResearchAnalysisService, SemanticKernelResearchAnalysisService>();
```

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit AI boundary**

Run:

```powershell
git add src/StockAgent.Api/Infrastructure/Ai src/StockAgent.Api/Program.cs
git commit -m "feat: add semantic kernel analysis boundary"
```

Expected: commit succeeds.

## Task 8: Report Generation And PDF Export

**Files:**
- Create: `src/StockAgent.Api/Infrastructure/Reports/ReportScore.cs`
- Create: `src/StockAgent.Api/Infrastructure/Reports/ReportGenerator.cs`
- Create: `src/StockAgent.Api/Infrastructure/Pdf/IPdfExportService.cs`
- Create: `src/StockAgent.Api/Infrastructure/Pdf/PlaywrightPdfExportService.cs`
- Modify: `src/StockAgent.Api/Program.cs`

- [ ] **Step 1: Implement report generator**

Create `src/StockAgent.Api/Infrastructure/Reports/ReportScore.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Structured score summary rendered at the top of a research report.
/// </summary>
public sealed record ReportScore(int OverallScore, string RiskLevel, string ValuationView, decimal Confidence);
```

Create `src/StockAgent.Api/Infrastructure/Reports/ReportGenerator.cs`:

```csharp
using System.Net;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Ai;
using StockAgent.Api.Infrastructure.DataSources;

namespace StockAgent.Api.Infrastructure.Reports;

/// <summary>
/// Converts bounded analysis output into Chinese Markdown and HTML research reports.
/// </summary>
public sealed class ReportGenerator
{
    /// <summary>
    /// Generates a readable Chinese report without direct buy/sell instructions.
    /// </summary>
    public GeneratedReport Generate(MarketDataSnapshot snapshot, AiAnalysisResult analysis, IReadOnlyList<EvidenceCard> evidenceCards)
    {
        var markdown = $"""
        # {snapshot.CompanyName} {snapshot.Ticker} 深度研究报告

        ## 评分结论

        - 综合评分：{analysis.OverallScore}/100
        - 风险等级：{analysis.RiskLevel}
        - 估值判断：{analysis.ValuationView}

        ## 核心摘要

        {analysis.Summary}

        ## 关键假设

        {string.Join(Environment.NewLine, analysis.KeyAssumptions.Select(x => $"- {x}"))}

        ## 来源证据

        {string.Join(Environment.NewLine, evidenceCards.Select(x => $"- {x.Claim}：{x.Snippet}"))}

        ## 风险提示

        本报告仅用于研究辅助，不构成买卖建议。数据和公开材料可能存在延迟、遗漏或解释偏差。
        """;

        var html = $"<article>{WebUtility.HtmlEncode(markdown).Replace("\n", "<br />")}</article>";
        var score = new ReportScore(analysis.OverallScore, analysis.RiskLevel, analysis.ValuationView, 0.72m);

        return new GeneratedReport(markdown, html, score);
    }
}

/// <summary>
/// Generated report payload before persistence.
/// </summary>
public sealed record GeneratedReport(string Markdown, string Html, ReportScore Score);
```

- [ ] **Step 2: Implement PDF service boundary**

Create `src/StockAgent.Api/Infrastructure/Pdf/IPdfExportService.cs`:

```csharp
namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// Exports generated report HTML to a PDF file.
/// </summary>
public interface IPdfExportService
{
    /// <summary>Writes a PDF file and returns the absolute path.</summary>
    Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken);
}
```

Create `src/StockAgent.Api/Infrastructure/Pdf/PlaywrightPdfExportService.cs`:

```csharp
using Microsoft.Playwright;

namespace StockAgent.Api.Infrastructure.Pdf;

/// <summary>
/// PDF exporter that renders report HTML through Playwright Chromium.
/// </summary>
public sealed class PlaywrightPdfExportService(IWebHostEnvironment environment) : IPdfExportService
{
    /// <inheritdoc />
    public async Task<string> ExportAsync(Guid researchTaskId, string html, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(environment.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot"), "pdf");
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, $"{researchTaskId}.pdf");

        await using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var page = await browser.NewPageAsync();

        // The print wrapper gives the PDF stable typography and spacing independent of the web UI.
        var document = $$"""
        <!doctype html>
        <html lang="zh-CN">
        <head>
          <meta charset="utf-8" />
          <style>
            body { font-family: "Microsoft YaHei", "Noto Sans CJK SC", Arial, sans-serif; margin: 32px; line-height: 1.65; color: #17202a; }
            article { max-width: 820px; margin: 0 auto; }
          </style>
        </head>
        <body>{{html}}</body>
        </html>
        """;

        await page.SetContentAsync(document, new PageSetContentOptions { WaitUntil = WaitUntilState.NetworkIdle });
        await page.PdfAsync(new PagePdfOptions
        {
            Path = filePath,
            Format = "A4",
            PrintBackground = true,
            Margin = new Margin { Top = "18mm", Right = "16mm", Bottom = "18mm", Left = "16mm" }
        });

        return filePath;
    }
}
```

- [ ] **Step 3: Register report and PDF services**

Modify `Program.cs`:

```csharp
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Reports;

builder.Services.AddScoped<ReportGenerator>();
builder.Services.AddScoped<IPdfExportService, PlaywrightPdfExportService>();
```

- [ ] **Step 4: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 5: Commit reports and PDF boundary**

Run:

```powershell
git add src/StockAgent.Api/Infrastructure/Reports src/StockAgent.Api/Infrastructure/Pdf src/StockAgent.Api/Program.cs
git commit -m "feat: add report generation and pdf boundary"
```

Expected: commit succeeds.

## Task 9: Report, Evidence, PDF, Settings, And Health Endpoints

**Files:**
- Create: `src/StockAgent.Api/Features/Reports/ReportEndpoints.cs`
- Create: `src/StockAgent.Api/Features/Evidence/EvidenceEndpoints.cs`
- Create: `src/StockAgent.Api/Features/Pdf/PdfEndpoints.cs`
- Create: `src/StockAgent.Api/Features/Settings/SettingsEndpoints.cs`
- Create: `src/StockAgent.Api/Features/Health/DataSourceHealthEndpoints.cs`
- Modify: `src/StockAgent.Api/Program.cs`

- [ ] **Step 1: Create report endpoint**

Create `src/StockAgent.Api/Features/Reports/ReportEndpoints.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Reports;

/// <summary>
/// Endpoints for reading generated research reports.
/// </summary>
public static class ReportEndpoints
{
    /// <summary>Maps report endpoints.</summary>
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/report", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            return report is null ? Results.NotFound() : Results.Ok(report);
        }).WithTags("Reports");

        return app;
    }
}
```

- [ ] **Step 2: Create evidence endpoint**

Create `src/StockAgent.Api/Features/Evidence/EvidenceEndpoints.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Evidence;

/// <summary>
/// Endpoints for reading evidence cards and source metadata.
/// </summary>
public static class EvidenceEndpoints
{
    /// <summary>Maps evidence endpoints.</summary>
    public static IEndpointRouteBuilder MapEvidenceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/research-tasks/{id:guid}/evidence", async (Guid id, StockAgentDbContext db, CancellationToken cancellationToken) =>
        {
            var cards = await db.EvidenceCards.Where(x => x.ResearchTaskId == id).ToListAsync(cancellationToken);
            return Results.Ok(cards);
        }).WithTags("Evidence");

        return app;
    }
}
```

- [ ] **Step 3: Create PDF endpoint**

Create `src/StockAgent.Api/Features/Pdf/PdfEndpoints.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Infrastructure.Pdf;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Features.Pdf;

/// <summary>
/// Endpoints for requesting and downloading PDF exports.
/// </summary>
public static class PdfEndpoints
{
    /// <summary>Maps PDF endpoints.</summary>
    public static IEndpointRouteBuilder MapPdfEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/research-tasks/{id:guid}/pdf", async (
            Guid id,
            StockAgentDbContext db,
            IPdfExportService pdfExportService,
            CancellationToken cancellationToken) =>
        {
            var report = await db.ResearchReports.FirstOrDefaultAsync(x => x.ResearchTaskId == id, cancellationToken);
            if (report is null)
            {
                return Results.NotFound();
            }

            var path = await pdfExportService.ExportAsync(id, report.Html, cancellationToken);
            return Results.Ok(new { researchTaskId = id, filePath = path, status = "Completed" });
        }).WithTags("PDF");

        return app;
    }
}
```

- [ ] **Step 4: Create settings and health endpoints**

Create settings and health endpoints returning deterministic first-version values:

```csharp
// File: src/StockAgent.Api/Features/Settings/SettingsEndpoints.cs
namespace StockAgent.Api.Features.Settings;

/// <summary>Endpoints for first-version provider and research settings.</summary>
public static class SettingsEndpoints
{
    /// <summary>Maps settings endpoints.</summary>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/settings/providers", () => Results.Ok(new { openAiEnabled = false, compatibleEnabled = true }))
            .WithTags("Settings");
        app.MapGet("/api/settings/research", () => Results.Ok(new { defaultLanguage = "zh-CN", maxEvidenceCards = 30 }))
            .WithTags("Settings");
        return app;
    }
}

// File: src/StockAgent.Api/Features/Health/DataSourceHealthEndpoints.cs
namespace StockAgent.Api.Features.Health;

/// <summary>Endpoints that expose first-version data source health.</summary>
public static class DataSourceHealthEndpoints
{
    /// <summary>Maps data-source health endpoints.</summary>
    public static IEndpointRouteBuilder MapDataSourceHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health/data-sources", () => Results.Ok(new[] { new { name = "FakeProvider", status = "Healthy" } }))
            .WithTags("Health");
        return app;
    }
}
```

- [ ] **Step 5: Register endpoints**

Modify `Program.cs`:

```csharp
using StockAgent.Api.Features.Evidence;
using StockAgent.Api.Features.Health;
using StockAgent.Api.Features.Pdf;
using StockAgent.Api.Features.Reports;
using StockAgent.Api.Features.Settings;

app.MapReportEndpoints();
app.MapEvidenceEndpoints();
app.MapPdfEndpoints();
app.MapSettingsEndpoints();
app.MapDataSourceHealthEndpoints();
```

- [ ] **Step 6: Build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: build succeeds.

- [ ] **Step 7: Commit endpoints**

Run:

```powershell
git add src/StockAgent.Api
git commit -m "feat: add report evidence pdf and settings endpoints"
```

Expected: commit succeeds.

## Task 10: React Frontend Shell

**Files:**
- Create: `src/StockAgent.Web/*`
- Create: `src/StockAgent.Web/src/models.ts`
- Create: `src/StockAgent.Web/src/api/researchApi.ts`
- Create: `src/StockAgent.Web/src/App.tsx`
- Create: `src/StockAgent.Web/src/main.tsx`
- Create: `src/StockAgent.Web/src/styles.css`

- [ ] **Step 1: Scaffold Vite React app**

Run:

```powershell
npm create vite@latest src/StockAgent.Web -- --template react-ts
```

Expected: Vite creates the React TypeScript project.

- [ ] **Step 2: Add frontend packages**

Run:

```powershell
Set-Location src/StockAgent.Web
npm install @tanstack/react-query react-router-dom @microsoft/signalr
npm install -D vitest @testing-library/react @testing-library/jest-dom jsdom
Set-Location ../..
```

Expected: packages are installed.

- [ ] **Step 3: Create shared frontend models**

Create `src/StockAgent.Web/src/models.ts`:

```ts
/**
 * Durable status values returned by the research task API.
 */
export type ResearchTaskStatus =
  | 'Queued'
  | 'Running'
  | 'CollectingData'
  | 'IngestingDocuments'
  | 'Analyzing'
  | 'GeneratingReport'
  | 'Ready'
  | 'ExportingPdf'
  | 'Completed'
  | 'Failed'
  | 'Cancelled';

/**
 * Research task summary shown in the workbench queue.
 */
export interface ResearchTask {
  id: string;
  ticker: string;
  market: 'AShare' | 'HongKong';
  status: ResearchTaskStatus;
  progressPercent: number;
  language: string;
}

/**
 * Report payload rendered by the report viewer.
 */
export interface ResearchReport {
  markdown: string;
  html: string;
  ratingJson: string;
}
```

- [ ] **Step 4: Create API client**

Create `src/StockAgent.Web/src/api/researchApi.ts`:

```ts
import type { ResearchReport, ResearchTask } from '../models';

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000';

/**
 * Creates a new stock research task.
 */
export async function createResearchTask(ticker: string, market: 'AShare' | 'HongKong'): Promise<ResearchTask> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ ticker, market, language: 'zh-CN' }),
  });

  if (!response.ok) {
    throw new Error(`Create research task failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads the task queue and history.
 */
export async function listResearchTasks(): Promise<ResearchTask[]> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks`);
  if (!response.ok) {
    throw new Error(`List research tasks failed with ${response.status}`);
  }

  return response.json();
}

/**
 * Loads a generated report for the selected task.
 */
export async function getResearchReport(taskId: string): Promise<ResearchReport> {
  const response = await fetch(`${apiBaseUrl}/api/research-tasks/${taskId}/report`);
  if (!response.ok) {
    throw new Error(`Get report failed with ${response.status}`);
  }

  return response.json();
}
```

- [ ] **Step 5: Create app shell**

Create `src/StockAgent.Web/src/App.tsx`:

```tsx
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ResearchWorkbench } from './components/ResearchWorkbench';
import './styles.css';

const queryClient = new QueryClient();

/**
 * Root frontend application for the stock research workbench.
 */
export function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <ResearchWorkbench />
    </QueryClientProvider>
  );
}
```

Create `src/StockAgent.Web/src/main.tsx`:

```tsx
import React from 'react';
import ReactDOM from 'react-dom/client';
import { App } from './App';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
);
```

- [ ] **Step 6: Commit frontend shell**

Run:

```powershell
git add src/StockAgent.Web
git commit -m "feat: add react workbench shell"
```

Expected: commit succeeds.

## Task 11: Workbench Components

**Files:**
- Create: `src/StockAgent.Web/src/components/ResearchWorkbench.tsx`
- Create: `src/StockAgent.Web/src/components/TaskTimeline.tsx`
- Create: `src/StockAgent.Web/src/components/ReportViewer.tsx`
- Create: `src/StockAgent.Web/src/components/EvidenceDrawer.tsx`
- Create: `src/StockAgent.Web/src/components/SettingsPage.tsx`
- Modify: `src/StockAgent.Web/src/styles.css`

- [ ] **Step 1: Create TaskTimeline**

Create `src/StockAgent.Web/src/components/TaskTimeline.tsx`:

```tsx
import type { ResearchTaskStatus } from '../models';

const stages: ResearchTaskStatus[] = ['Queued', 'CollectingData', 'IngestingDocuments', 'Analyzing', 'GeneratingReport', 'Ready', 'ExportingPdf', 'Completed'];

/**
 * Displays first-version research progress as a compact horizontal timeline.
 */
export function TaskTimeline({ status }: { status: ResearchTaskStatus }) {
  const activeIndex = Math.max(0, stages.indexOf(status));

  return (
    <ol className="timeline">
      {stages.map((stage, index) => (
        <li key={stage} className={index <= activeIndex ? 'timelineItem active' : 'timelineItem'}>
          {stage}
        </li>
      ))}
    </ol>
  );
}
```

- [ ] **Step 2: Create ReportViewer**

Create `src/StockAgent.Web/src/components/ReportViewer.tsx`:

```tsx
import type { ResearchReport } from '../models';

/**
 * Renders a generated research report and exposes the PDF export action.
 */
export function ReportViewer({ report, onExportPdf }: { report?: ResearchReport; onExportPdf: () => void }) {
  if (!report) {
    return <section className="emptyState">选择一个已完成任务后查看报告。</section>;
  }

  return (
    <section className="reportViewer">
      <div className="reportToolbar">
        <h2>研究报告</h2>
        <button type="button" onClick={onExportPdf}>导出 PDF</button>
      </div>
      <article dangerouslySetInnerHTML={{ __html: report.html }} />
    </section>
  );
}
```

- [ ] **Step 3: Create ResearchWorkbench**

Create `src/StockAgent.Web/src/components/ResearchWorkbench.tsx`:

```tsx
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { createResearchTask, listResearchTasks } from '../api/researchApi';
import type { ResearchTask } from '../models';
import { ReportViewer } from './ReportViewer';
import { TaskTimeline } from './TaskTimeline';

/**
 * Main first-screen workbench for submitting stock research tasks and reading reports.
 */
export function ResearchWorkbench() {
  const queryClient = useQueryClient();
  const [ticker, setTicker] = useState('00700.HK');
  const [market, setMarket] = useState<'AShare' | 'HongKong'>('HongKong');
  const [selectedTask, setSelectedTask] = useState<ResearchTask | undefined>();

  const tasksQuery = useQuery({ queryKey: ['researchTasks'], queryFn: listResearchTasks, refetchInterval: 3000 });
  const createMutation = useMutation({
    mutationFn: () => createResearchTask(ticker, market),
    onSuccess: async (task) => {
      setSelectedTask(task);
      await queryClient.invalidateQueries({ queryKey: ['researchTasks'] });
    },
  });

  return (
    <main className="workbench">
      <aside className="sidebar">
        <h1>Stock Research Agent</h1>
        <label>
          股票代码
          <input value={ticker} onChange={(event) => setTicker(event.target.value)} />
        </label>
        <label>
          市场
          <select value={market} onChange={(event) => setMarket(event.target.value as 'AShare' | 'HongKong')}>
            <option value="AShare">A 股</option>
            <option value="HongKong">港股</option>
          </select>
        </label>
        <button type="button" onClick={() => createMutation.mutate()} disabled={createMutation.isPending}>
          开始研究
        </button>
        <div className="taskList">
          {(tasksQuery.data ?? []).map((task) => (
            <button key={task.id} type="button" onClick={() => setSelectedTask(task)}>
              {task.ticker} · {task.status}
            </button>
          ))}
        </div>
      </aside>
      <section className="content">
        {selectedTask && <TaskTimeline status={selectedTask.status} />}
        <ReportViewer report={undefined} onExportPdf={() => undefined} />
      </section>
    </main>
  );
}
```

- [ ] **Step 4: Add focused CSS**

Create `src/StockAgent.Web/src/styles.css`:

```css
body {
  margin: 0;
  font-family: Inter, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
  background: #f6f7f9;
  color: #17202a;
}

button,
input,
select {
  font: inherit;
}

.workbench {
  display: grid;
  grid-template-columns: 280px minmax(0, 1fr);
  min-height: 100vh;
}

.sidebar {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 20px;
  border-right: 1px solid #d9dee7;
  background: #ffffff;
}

.content {
  padding: 20px;
}

.timeline {
  display: flex;
  gap: 8px;
  padding: 0;
  list-style: none;
}

.timelineItem {
  padding: 6px 8px;
  border: 1px solid #d9dee7;
  border-radius: 6px;
  color: #5d6b7a;
}

.timelineItem.active {
  border-color: #1769aa;
  color: #0d3f67;
  background: #e9f3fb;
}
```

- [ ] **Step 5: Run frontend build**

Run:

```powershell
Set-Location src/StockAgent.Web
npm run build
Set-Location ../..
```

Expected: Vite build succeeds.

- [ ] **Step 6: Commit workbench UI**

Run:

```powershell
git add src/StockAgent.Web
git commit -m "feat: add research workbench UI"
```

Expected: commit succeeds.

## Task 12: Local Runtime And Verification

**Files:**
- Create: `docker-compose.yml`
- Modify: `src/StockAgent.Api/appsettings.Development.json`

- [ ] **Step 1: Create Docker Compose for PostgreSQL**

Create `docker-compose.yml`:

```yaml
services:
  postgres:
    image: postgres:16
    environment:
      POSTGRES_DB: stockagent
      POSTGRES_USER: stockagent
      POSTGRES_PASSWORD: stockagent
    ports:
      - "5432:5432"
    volumes:
      - stockagent-postgres:/var/lib/postgresql/data

volumes:
  stockagent-postgres:
```

- [ ] **Step 2: Configure development connection string**

Modify `src/StockAgent.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "StockAgent": "Host=localhost;Port=5432;Database=stockagent;Username=stockagent;Password=stockagent"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

- [ ] **Step 3: Run backend tests**

Run:

```powershell
dotnet test StockAgent.sln
```

Expected: all backend tests pass.

- [ ] **Step 4: Install Playwright Chromium**

Run after the API project has been built once:

```powershell
powershell -ExecutionPolicy Bypass -File src\StockAgent.Api\bin\Debug\net10.0\playwright.ps1 install chromium
```

Expected: Chromium browser binaries are installed for Microsoft.Playwright.

- [ ] **Step 5: Run frontend build**

Run:

```powershell
Set-Location src/StockAgent.Web
npm run build
Set-Location ../..
```

Expected: frontend build passes.

- [ ] **Step 6: Run full build**

Run:

```powershell
dotnet build StockAgent.sln
```

Expected: backend build passes.

- [ ] **Step 7: Commit runtime setup**

Run:

```powershell
git add docker-compose.yml src/StockAgent.Api/appsettings.Development.json
git commit -m "chore: add local runtime configuration"
```

Expected: commit succeeds.

## Self-Review Checklist

- Spec coverage:
  - .NET 10 backend: Tasks 1-9 and 12.
  - React workbench: Tasks 10-11.
  - Task queue and background worker: Tasks 4-5.
  - Semantic Kernel direction: Task 7.
  - Provider abstraction: Tasks 5 and 7.
  - Evidence/context controls: Task 6.
  - Report and PDF: Tasks 8-9.
  - Full comments: every code snippet includes public XML comments or TSDoc.
- Placeholder scan:
  - No task contains a vague implementation step.
  - External services are represented by deterministic first-version providers.
- Type consistency:
  - `ResearchTaskStatus`, `Market`, `ResearchTaskResponse`, and API client model names are consistent across backend and frontend.
  - `IResearchTaskQueue`, `ResearchTaskQueue`, and `ResearchWorker` signatures align.
  - `IResearchAnalysisService` consumes `MarketDataSnapshot` and `EvidenceCard` as defined earlier.
