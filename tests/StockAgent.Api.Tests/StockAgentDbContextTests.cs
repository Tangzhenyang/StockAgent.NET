using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Tests for the research persistence model.
/// </summary>
public sealed class StockAgentDbContextTests
{
    /// <summary>
    /// Verifies required entities and indexes are configured in the EF Core model.
    /// </summary>
    [Fact]
    public void Model_contains_research_entities_and_key_indexes()
    {
        var options = new DbContextOptionsBuilder<StockAgentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var dbContext = new StockAgentDbContext(options);
        var model = dbContext.Model;

        model.FindEntityType(typeof(ResearchTask)).Should().NotBeNull();
        model.FindEntityType(typeof(ResearchStep)).Should().NotBeNull();
        model.FindEntityType(typeof(DocumentSource)).Should().NotBeNull();
        model.FindEntityType(typeof(DocumentChunk)).Should().NotBeNull();
        model.FindEntityType(typeof(EvidenceCard)).Should().NotBeNull();
        model.FindEntityType(typeof(ResearchReport)).Should().NotBeNull();
        model.FindEntityType(typeof(PdfExport)).Should().NotBeNull();
        model.FindEntityType(typeof(ModelInvocation)).Should().NotBeNull();
        model.FindEntityType(typeof(AppSetting)).Should().NotBeNull();

        var documentSource = model.FindEntityType(typeof(DocumentSource));
        documentSource!.FindProperty(nameof(DocumentSource.Url))!.GetMaxLength().Should().Be(2048);
        var documentSourceIndexProperties = new[] { nameof(DocumentSource.ResearchTaskId), nameof(DocumentSource.ContentHash) };
        documentSource.GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(documentSourceIndexProperties));

        var documentChunk = model.FindEntityType(typeof(DocumentChunk));
        var documentChunkIndexProperties = new[] { nameof(DocumentChunk.DocumentSourceId), nameof(DocumentChunk.ChunkIndex) };
        documentChunk!.GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(documentChunkIndexProperties));
    }
}
