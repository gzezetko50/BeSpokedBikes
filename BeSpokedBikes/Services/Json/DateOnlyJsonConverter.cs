using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeSpokedBikes.Services.Json
{
    // Accept multiple common date formats for DateOnly
    public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        private static readonly string[] Formats = new[]
        {
            "yyyy-MM-dd",
            "MM/dd/yyyy",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ss.fff"
        };

        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString();
                if (string.IsNullOrWhiteSpace(s))
                    throw new FormatException("Empty date string.");

                // Try all formats
                foreach (var fmt in Formats)
                {
                    if (DateTime.TryParseExact(s, fmt, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var dt))
                    {
                        return DateOnly.FromDateTime(dt);
                    }
                }

                // Fallback: general parse (handles ISO with timezone)
                if (DateTime.TryParse(s, out var anyDt))
                    return DateOnly.FromDateTime(anyDt);
            }

            throw new FormatException("The JSON value is not in a supported DateOnly format.");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            // Always emit as ISO yyyy-MM-dd
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }
}