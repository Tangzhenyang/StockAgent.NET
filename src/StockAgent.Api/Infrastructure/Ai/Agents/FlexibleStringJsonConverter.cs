using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Lenient converter for text fields that LLMs may return as objects, arrays, numbers, or booleans.
/// 面向大模型文本字段的宽松转换器，兼容对象、数组、数字或布尔值。
/// </summary>
public sealed class FlexibleStringJsonConverter : JsonConverter<string>
{
    /// <inheritdoc />
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString() ?? string.Empty;
        }

        if (reader.TokenType is JsonTokenType.Null)
        {
            return string.Empty;
        }

        using var document = JsonDocument.ParseValue(ref reader);
        return RenderElement(document.RootElement);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }

    private static string RenderElement(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => RenderObject(element),
            JsonValueKind.Array => string.Join("；", element.EnumerateArray().Select(RenderElement).Where(static x => x.Length > 0)),
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            _ => string.Empty
        };
    }

    private static string RenderObject(JsonElement element)
    {
        var builder = new StringBuilder();
        foreach (var property in element.EnumerateObject())
        {
            var value = RenderElement(property.Value);
            if (value.Length == 0)
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append("；");
            }

            builder.Append(property.Name);
            builder.Append('：');
            builder.Append(value);
        }

        return builder.ToString();
    }
}
