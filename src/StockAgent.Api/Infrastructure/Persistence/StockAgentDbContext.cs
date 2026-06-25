using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using StockAgent.Api.Domain;

namespace StockAgent.Api.Infrastructure.Persistence;

/// <summary>
/// EF Core database context for research tasks, evidence, reports, and provider audit records.
/// 用于研究任务、证据、报告和提供器审计记录的 EF Core 数据库上下文。
/// </summary>
public sealed class StockAgentDbContext(DbContextOptions<StockAgentDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    /// <summary>Research task roots. 研究任务根实体。</summary>
    public DbSet<ResearchTask> ResearchTasks => Set<ResearchTask>();
    /// <summary>Research stage audit records. 研究阶段审计记录。</summary>
    public DbSet<ResearchStep> ResearchSteps => Set<ResearchStep>();
    /// <summary>Structured step artifacts for expandable diagnostics. 用于可展开诊断的结构化步骤产物。</summary>
    public DbSet<ResearchStepArtifact> ResearchStepArtifacts => Set<ResearchStepArtifact>();
    /// <summary>Collected source documents. 收集到的源文档。</summary>
    public DbSet<DocumentSource> DocumentSources => Set<DocumentSource>();
    /// <summary>Parsed document chunks. 解析后的文档块。</summary>
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();
    /// <summary>Compressed evidence cards. 压缩后的证据卡。</summary>
    public DbSet<EvidenceCard> EvidenceCards => Set<EvidenceCard>();
    /// <summary>Generated reports. 生成的报告。</summary>
    public DbSet<ResearchReport> ResearchReports => Set<ResearchReport>();
    /// <summary>PDF export records. PDF 导出记录。</summary>
    public DbSet<PdfExport> PdfExports => Set<PdfExport>();
    /// <summary>Model invocation audit records. 模型调用审计记录。</summary>
    public DbSet<ModelInvocation> ModelInvocations => Set<ModelInvocation>();
    /// <summary>Application settings stored as JSON strings. 以 JSON 字符串存储的应用设置。</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    /// <summary>User settings stored as JSON strings. 以 JSON 字符串存储的用户设置。</summary>
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.Property(x => x.UpdatedAt).IsRequired();
        });

        modelBuilder.Entity<ResearchTask>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.Ticker).HasMaxLength(32).IsRequired();
            entity.Property(x => x.CompanyName).HasMaxLength(256);
            entity.Property(x => x.Language).HasMaxLength(16).IsRequired();
            entity.HasIndex(x => x.UserId);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasMany(x => x.Steps).WithOne(x => x.ResearchTask).HasForeignKey(x => x.ResearchTaskId);
        });

        modelBuilder.Entity<ResearchStep>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.InputSummary).HasMaxLength(2000);
            entity.Property(x => x.OutputSummary).HasMaxLength(2000);
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
        });

        modelBuilder.Entity<ResearchStepArtifact>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ArtifactType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Title).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Summary).HasMaxLength(1000);
            entity.Property(x => x.JsonPayload).IsRequired();
            entity.HasIndex(x => new { x.ResearchTaskId, x.ResearchStepId });
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

        modelBuilder.Entity<UserSetting>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.SettingKey).HasMaxLength(128).IsRequired();
            entity.Property(x => x.SettingValueJson).IsRequired();
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            entity.HasIndex(x => new { x.UserId, x.SettingKey }).IsUnique();
        });

        modelBuilder.Entity<ModelInvocation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.StepName).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Provider).HasMaxLength(128).IsRequired();
            entity.Property(x => x.ModelName).HasMaxLength(256).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ErrorMessage).HasMaxLength(4000);
            entity.HasIndex(x => x.ResearchTaskId);
        });
    }
}
