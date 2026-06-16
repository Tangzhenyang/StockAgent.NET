namespace StockAgent.Api.Infrastructure.Documents;

/// <summary>
/// Splits long source text into bounded chunks so raw documents are never sent wholesale to a model.
/// </summary>
public sealed class DocumentChunker
{
    /// <summary>
    /// Splits text into character-bounded chunks.
    /// </summary>
    /// <param name="text">Raw source text to split.</param>
    /// <param name="maxCharacters">Maximum number of characters allowed in one chunk.</param>
    /// <returns>Ordered chunks with rough token estimates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the chunk size is not positive.</exception>
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
/// <param name="Index">Zero-based chunk index.</param>
/// <param name="Text">Chunk text.</param>
/// <param name="TokenEstimate">Rough token estimate for budgeting.</param>
public sealed record DocumentTextChunk(int Index, string Text, int TokenEstimate);
