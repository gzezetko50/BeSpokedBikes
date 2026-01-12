using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeSpokedBikes.Services.Json
{
    // Converts a JSON array of mixed phone entries (strings or objects) to List<string>.
    // For objects, tries "number" then "phone"; skips unknown shapes.
    public sealed class PhoneListConverter : JsonConverter<List<string>>
    {
        public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var result = new List<string>();

            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException("Expected array for phones.");

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray) break;

                if (reader.TokenType == JsonTokenType.String)
                {
                    result.Add(reader.GetString() ?? string.Empty);
                }
                else if (reader.TokenType == JsonTokenType.StartObject)
                {
                    using var doc = JsonDocument.ParseValue(ref reader);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("number", out var num) && num.ValueKind == JsonValueKind.String)
                        result.Add(num.GetString() ?? string.Empty);
                    else if (root.TryGetProperty("phone", out var ph) && ph.ValueKind == JsonValueKind.String)
                        result.Add(ph.GetString() ?? string.Empty);
                    // Unknown object shape: ignore safely
                }
                else
                {
                    // Ignore unexpected token types
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            foreach (var s in value) writer.WriteStringValue(s);
            writer.WriteEndArray();
        }
    }
}