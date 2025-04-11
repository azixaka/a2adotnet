using System.Text.Json;
using System.Text.Json.Serialization;

namespace A2Adotnet.Common.Protocol.Messages;

/// <summary>
/// Converts a <see cref="RequestId"/> to and from JSON, handling string or number types.
/// </summary>
public class RequestIdConverter : JsonConverter<RequestId>
{
    public override RequestId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.String => new RequestId(reader.GetString()!),
            JsonTokenType.Number => new RequestId(reader.GetInt64()),
            _ => throw new JsonException($"Unexpected token type {reader.TokenType} for RequestId.")
        };
    }

    public override void Write(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
    {
        if (value.StringValue != null)
        {
            writer.WriteStringValue(value.StringValue);
        }
        else if (value.LongValue.HasValue)
        {
            writer.WriteNumberValue(value.LongValue.Value);
        }
        else
        {
            // Should not happen with a properly constructed RequestId
            writer.WriteNullValue();
        }
    }

    // Optional: Handle reading nullable RequestId?
    public override RequestId ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Property names must be strings in JSON
        return new RequestId(reader.GetString()!);
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, RequestId value, JsonSerializerOptions options)
    {
        // Property names must be strings in JSON
        writer.WritePropertyName(value.ToString());
    }
}