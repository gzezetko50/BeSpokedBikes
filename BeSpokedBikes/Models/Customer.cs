using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BeSpokedBikes.Services.Json;

namespace BeSpokedBikes.Models
{
    public class Customer
    {
        [JsonPropertyName("customerId")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        // New: startDate and statusId from API
        [JsonPropertyName("startDate")]
        public DateOnly? StartDate { get; set; }

        [JsonPropertyName("statusId")]
        public int? StatusId { get; set; }

        // Addresses can be strings OR objects
        [JsonPropertyName("addresses")]
        public List<JsonElement> Addresses { get; set; } = new();

        [JsonIgnore]
        public string Address
        {
            get
            {
                if (Addresses.Count == 0) return string.Empty;
                var el = Addresses[0];

                if (el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                if (el.ValueKind == JsonValueKind.Object)
                {
                    // Handle structured address like:
                    // {"streetAddress1":"855 Aspen Blvd","streetAddress2":null,
                    //  "city":"San Diego","state":"CA","zip":"92101","country":"USA",...}
                    if (el.TryGetProperty("streetAddress1", out var street1))
                    {
                        var parts = new List<string>();

                        var s1 = street1.GetString();
                        if (!string.IsNullOrWhiteSpace(s1))
                            parts.Add(s1);

                        if (el.TryGetProperty("streetAddress2", out var street2))
                        {
                            var s2 = street2.GetString();
                            if (!string.IsNullOrWhiteSpace(s2))
                                parts.Add(s2);
                        }

                        string? city = el.TryGetProperty("city", out var cityEl) ? cityEl.GetString() : null;
                        string? state = el.TryGetProperty("state", out var stateEl) ? stateEl.GetString() : null;
                        string? zip = el.TryGetProperty("zip", out var zipEl) ? zipEl.GetString() : null;

                        var cityStateZip = string.Join(", ",
                            new[]
                            {
                                city,
                                string.IsNullOrWhiteSpace(state) ? null : state,
                                string.IsNullOrWhiteSpace(zip) ? null : zip
                            }.Where(x => !string.IsNullOrWhiteSpace(x)));

                        if (!string.IsNullOrWhiteSpace(cityStateZip))
                            parts.Add(cityStateZip);

                        if (el.TryGetProperty("country", out var countryEl))
                        {
                            var country = countryEl.GetString();
                            if (!string.IsNullOrWhiteSpace(country))
                                parts.Add(country);
                        }

                        return string.Join(", ", parts);
                    }

                    // Fallback to simpler shapes
                    if (el.TryGetProperty("line1", out var line1) && line1.ValueKind == JsonValueKind.String)
                        return line1.GetString() ?? string.Empty;
                    if (el.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.String)
                        return addr.GetString() ?? string.Empty;
                }

                return el.ToString();
            }
        }

        // Phones can be strings OR objects, so use JsonElement list
        [JsonPropertyName("phones")]
        public List<JsonElement> Phones { get; set; } = new();

        [JsonIgnore]
        public string Phone
        {
            get
            {
                if (Phones.Count == 0) return string.Empty;
                var el = Phones[0];

                if (el.ValueKind == JsonValueKind.String)
                    return el.GetString() ?? string.Empty;

                if (el.ValueKind == JsonValueKind.Object)
                {
                    // Common field names from APIs
                    if (el.TryGetProperty("number", out var num) && num.ValueKind == JsonValueKind.String)
                        return num.GetString() ?? string.Empty;

                    if (el.TryGetProperty("phone", out var ph) && ph.ValueKind == JsonValueKind.String)
                        return ph.GetString() ?? string.Empty;

                    if (el.TryGetProperty("phoneNumber", out var pn) && pn.ValueKind == JsonValueKind.String)
                    {
                        var value = pn.GetString() ?? string.Empty;

                        if (el.TryGetProperty("extension", out var ext) && ext.ValueKind == JsonValueKind.String)
                        {
                            var extStr = ext.GetString();
                            if (!string.IsNullOrWhiteSpace(extStr))
                                return $"{value} x{extStr}";
                        }

                        return value;
                    }
                }

                // Fallback: raw JSON
                return el.ToString();
            }
        }
    }
}
