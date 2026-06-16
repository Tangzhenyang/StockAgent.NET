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
