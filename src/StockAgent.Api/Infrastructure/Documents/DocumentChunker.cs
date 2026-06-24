namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Splits long source text into bounded chunks so raw documents are never sent wholesale to a model.
/// 将较长的源文本拆分为受限块，避免原始文档整体发送给模型。
/// </summary>
public sealed class DocumentChunker
{
    /// <summary>
    /// Splits text into character-bounded chunks.
    /// 将文本拆分为按字符数限制的块。
    /// </summary>
    /// <param name="text">Raw source text to split. 要拆分的原始源文本。</param>
    /// <param name="maxCharacters">Maximum number of characters allowed in one chunk. 单个块允许的最大字符数。</param>
    /// <returns>Ordered chunks with rough token estimates. 带有粗略 token 估算的有序块。</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the chunk size is not positive. 当块大小不是正数时抛出。</exception>
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
/// 作为 DocumentChunk 实体持久化之前生成的内存块。
/// </summary>
/// <param name="Index">Zero-based chunk index. 从零开始的块索引。</param>
/// <param name="Text">Chunk text. 块文本。</param>
/// <param name="TokenEstimate">Rough token estimate for budgeting. 用于预算的粗略 token 估算值。</param>
public sealed record DocumentTextChunk(int Index, string Text, int TokenEstimate);
