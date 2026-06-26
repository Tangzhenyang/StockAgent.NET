using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Lenient converter for string list fields that LLMs may return as a string, object, or mixed array.
/// 面向大模型字符串列表字段的宽松转换器，兼容字符串、对象或混合数组。
/// </summary>
public sealed partial class FlexibleStringListJsonConverter : JsonConverter<IReadOnlyList<string>>
{
    /// <inheritdoc />
    public override IReadOnlyList<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Null)
        {
            return [];
        }

        if (reader.TokenType is JsonTokenType.StartArray)
        {
            return ReadArray(ref reader);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return SplitListText(RenderElement(document.RootElement));
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IReadOnlyList<string> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item);
        }

        writer.WriteEndArray();
    }

    private static IReadOnlyList<string> ReadArray(ref Utf8JsonReader reader)
    {
        var values = new List<string>();
        while (reader.Read())
        {
            if (reader.TokenType is JsonTokenType.EndArray)
            {
                return values;
            }

            using var item = JsonDocument.ParseValue(ref reader);
            values.AddRange(SplitListText(RenderElement(item.RootElement)));
        }

        return values;
    }

    private static IReadOnlyList<string> SplitListText(string text)
    {
        return ListSeparatorRegex()
            .Split(text)
            .Select(static x => x.Trim().Trim('-', '*', '•', ' ', '\t'))
            .Where(static x => x.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    private static string RenderElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Array => string.Join("；", element.EnumerateArray().Select(RenderElement)),
            JsonValueKind.Object => string.Join("；", element.EnumerateObject().Select(property => RenderElement(property.Value))),
            _ => string.Empty
        };
    }

    [GeneratedRegex(@"[；;。\n\r]+|(?<=\S)\s*[、]\s*")]
    private static partial Regex ListSeparatorRegex();
}
