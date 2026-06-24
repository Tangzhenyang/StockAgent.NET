using FluentAssertions;
using StockAgent.Api.Infrastructure.Documents;

namespace StockAgent.Api.Tests;

/// <summary>
/// Verifies bounded text chunking for long public documents.
/// 验证对较长公开文档进行受限文本分块。
/// </summary>
public sealed class DocumentChunkerTests
{
    /// <summary>
    /// Long text is split into chunks that respect the configured character budget.
    /// 长文本会被拆分为符合配置字符预算的块。
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
