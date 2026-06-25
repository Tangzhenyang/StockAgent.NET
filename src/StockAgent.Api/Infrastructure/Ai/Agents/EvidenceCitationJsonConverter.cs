using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Lenient converter for LLM citation outputs that may be strings or partial objects.
/// 面向大模型引用输出的宽松转换器，兼容字符串或字段不完整的对象。
/// </summary>
public sealed class EvidenceCitationJsonConverter : JsonConverter<EvidenceCitation>
{
    /// <inheritdoc />
    public override EvidenceCitation Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString() ?? string.Empty;
            return new EvidenceCitation(Guid.Empty, text, text, null);
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            using var skipped = JsonDocument.ParseValue(ref reader);
            return new EvidenceCitation(Guid.Empty, string.Empty, string.Empty, null);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        var evidenceCardId = ReadGuid(root, "evidenceCardId");
        var title = ReadString(root, "title");
        var snippet = ReadString(root, "snippet");
        var sourceDate = ReadDateTimeOffset(root, "sourceDate");

        return new EvidenceCitation(evidenceCardId, title, snippet, sourceDate);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, EvidenceCitation value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("evidenceCardId", value.EvidenceCardId);
        writer.WriteString("title", value.Title);
        writer.WriteString("snippet", value.Snippet);
        if (value.SourceDate is null)
        {
            writer.WriteNull("sourceDate");
        }
        else
        {
            writer.WriteString("sourceDate", value.SourceDate.Value);
        }

        writer.WriteEndObject();
    }

    private static Guid ReadGuid(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property))
        {
            return Guid.Empty;
        }

        if (property.ValueKind == JsonValueKind.String
            && Guid.TryParse(property.GetString(), out var parsed))
        {
            return parsed;
        }

        return Guid.Empty;
    }

    private static string ReadString(JsonElement root, string propertyName)
    {
        return root.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;
    }

    private static DateTimeOffset? ReadDateTimeOffset(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(property.GetString(), out var parsed) ? parsed : null;
    }
}
