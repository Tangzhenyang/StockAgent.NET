using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using StockAgent.Api.Domain;
using StockAgent.Api.Infrastructure.Persistence;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies the research persistence model.
/// 验证研究持久化模型。
/// </summary>
public sealed class StockAgentDbContextTests
{
    /// <summary>
    /// Verifies required entities and indexes are configured in the EF Core model.
    /// 验证 EF Core 模型中已配置所需实体和索引。
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
        model.FindEntityType(typeof(ApplicationUser)).Should().NotBeNull();
        model.FindEntityType(typeof(UserSetting)).Should().NotBeNull();

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

        var researchTask = model.FindEntityType(typeof(ResearchTask));
        var researchTaskUserIndexProperties = new[] { nameof(ResearchTask.UserId) };
        researchTask!.FindProperty(nameof(ResearchTask.UserId)).Should().NotBeNull();
        researchTask.GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(researchTaskUserIndexProperties));

        var userSetting = model.FindEntityType(typeof(UserSetting));
        var userSettingIndexProperties = new[] { nameof(UserSetting.UserId), nameof(UserSetting.SettingKey) };
        userSetting!.FindProperty(nameof(UserSetting.SettingKey))!.GetMaxLength().Should().Be(128);
        userSetting.GetIndexes()
            .Should()
            .ContainSingle(index =>
                index.IsUnique &&
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(userSettingIndexProperties));
    }
}
