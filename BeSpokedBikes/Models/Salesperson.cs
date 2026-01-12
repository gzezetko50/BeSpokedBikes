using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeSpokedBikes.Models
{
    public partial class Salesperson
    {
        private DateOnly startDate;

        [JsonPropertyName("salespersonId")]
        public int Id { get; set; }

        [JsonPropertyName("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [JsonPropertyName("lastName")]
        public string LastName { get; set; } = string.Empty;

        [JsonPropertyName("startDate")]
        public DateOnly StartDate { get => startDate; set => startDate = value; }

        [JsonPropertyName("terminationDate")]
        public DateOnly? TerminationDate { get; set; }

        [JsonPropertyName("managerId")]
        public int? ManagerId { get; set; }

        [JsonPropertyName("statusId")]
        public int? StatusId { get; set; }

        // Accept both strings and objects to avoid JsonException when addresses are structured
        [JsonPropertyName("addresses")]
        public List<JsonElement> Addresses { get; set; } = new();

        // Accept both strings and objects to avoid JsonException when phones are structured
        [JsonPropertyName("phones")]
        public List<JsonElement> Phones { get; set; } = new();

        // Convenience getters for UI
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
                    // Handle rich address object:
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

                    // Fallback to older/simple shapes
                    if (el.TryGetProperty("line1", out var line1) && line1.ValueKind == JsonValueKind.String)
                        return line1.GetString() ?? string.Empty;
                    if (el.TryGetProperty("address", out var addr) && addr.ValueKind == JsonValueKind.String)
                        return addr.GetString() ?? string.Empty;
                }

                // Last resort: raw JSON
                return el.ToString();
            }
        }

        [JsonIgnore]
        public string Phone
        {
            get
            {
                if (Phones.Count == 0) return string.Empty;
                var el = Phones[0];
                if (el.ValueKind == JsonValueKind.String) return el.GetString() ?? string.Empty;
                if (el.ValueKind == JsonValueKind.Object)
                {
                    // Common field names from various APIs
                    if (el.TryGetProperty("number", out var num) && num.ValueKind == JsonValueKind.String)
                        return num.GetString() ?? string.Empty;
                    if (el.TryGetProperty("phone", out var ph) && ph.ValueKind == JsonValueKind.String)
                        return ph.GetString() ?? string.Empty;
                    if (el.TryGetProperty("phoneNumber", out var pn) && pn.ValueKind == JsonValueKind.String)
                    {
                        var value = pn.GetString() ?? string.Empty;
                        // Append extension if present
                        if (el.TryGetProperty("extension", out var ext) && ext.ValueKind == JsonValueKind.String)
                        {
                            var extStr = ext.GetString();
                            if (!string.IsNullOrWhiteSpace(extStr))
                                return $"{value} x{extStr}";
                        }
                        return value;
                    }
                }
                return el.ToString();
            }
        }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
    }
}