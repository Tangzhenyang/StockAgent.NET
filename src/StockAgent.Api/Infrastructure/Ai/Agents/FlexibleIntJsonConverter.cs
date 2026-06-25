using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StockAgent.Api.Infrastructure.Ai.Agents;

/// <summary>
/// Lenient converter for numeric values that LLMs may return as strings with units.
/// 面向大模型数字输出的宽松转换器，兼容带单位的字符串数字。
/// </summary>
public sealed class FlexibleIntJsonConverter : JsonConverter<int>
{
    /// <inheritdoc />
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var integer))
            {
                return integer;
            }

            if (reader.TryGetDecimal(out var number))
            {
                return Convert.ToInt32(decimal.Round(number, 0, MidpointRounding.AwayFromZero));
            }
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString()?.Trim() ?? string.Empty;
            if (TryParseFlexibleInteger(value, out var integer))
            {
                return integer;
            }
        }

        throw new JsonException("Expected an integer number or a string containing an integer number.");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

    private static bool TryParseFlexibleInteger(string value, out int integer)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
        {
            return true;
        }

        if (decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalValue))
        {
            integer = Convert.ToInt32(decimal.Round(decimalValue, 0, MidpointRounding.AwayFromZero));
            return true;
        }

        var numericSpan = FindFirstNumericSpan(value);
        if (numericSpan.Length == 0)
        {
            integer = 0;
            return false;
        }

        if (decimal.TryParse(numericSpan, NumberStyles.Number, CultureInfo.InvariantCulture, out decimalValue))
        {
            integer = Convert.ToInt32(decimal.Round(decimalValue, 0, MidpointRounding.AwayFromZero));
            return true;
        }

        integer = 0;
        return false;
    }

    private static string FindFirstNumericSpan(string value)
    {
        var start = -1;
        for (var index = 0; index < value.Length; index++)
        {
            if (char.IsDigit(value[index]) || value[index] is '-' or '+')
            {
                start = index;
                break;
            }
        }

        if (start < 0)
        {
            return string.Empty;
        }

        var end = start;
        while (end < value.Length && (char.IsDigit(value[end]) || value[end] is '.' or '-' or '+'))
        {
            end++;
        }

        return value[start..end];
    }
}
