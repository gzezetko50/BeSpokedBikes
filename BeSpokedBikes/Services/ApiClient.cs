using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services.Json;
using Microsoft.Extensions.Logging; // added

namespace BeSpokedBikes.Services
{
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<ApiClient> _logger; // added
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static ApiClient()
        {
            JsonOptions.Converters.Add(new DateOnlyJsonConverter());
        }

        // Updated ctor to accept logger
        public ApiClient(HttpClient http, ILogger<ApiClient> logger)
        {
            _http = http;
            _logger = logger;
        }

        public record LoginRequest(string username, string password);
        public record LoginResponse(string token, string? redirect);

        // DTOs for create/update salesperson
        public record SalespersonUpsertDto(
            string firstName,
            string lastName,
            DateOnly startDate,
            DateOnly? terminationDate,
            int? managerId,
            int? statusId,
            IEnumerable<string>? addresses,
            IEnumerable<string>? phones
        );

        // DTO for creating/updating a product (align with API contract).
        // manufacturerId is non-nullable on the server, send 0 when unknown.
        public record ProductUpsertDto(
            string name,
            int manufacturerId,
            string? manufacturerName,
            int? styleId,
            string? styleName,
            decimal purchasePrice,
            decimal salePrice,
            int qtyOnHand,
            decimal commissionPercentage,
            int? statusId
        );

        // DTO for creating/updating a customer (matches call-sites in Pages)
        public record CustomerUpsertDto(
            string firstName,
            string lastName,
            string email,
            DateOnly? startDate,
            IEnumerable<string>? addresses,
            IEnumerable<string>? phones,
            int? statusId
        );

        // --------------------------------------------------------------------
        // Auth
        // --------------------------------------------------------------------
        public async Task<LoginResponse> LoginAsync(string username, string password)
        {
            var payload = new LoginRequest(username, password);
            using var resp = await _http.PostAsJsonAsync("/api/auth/login", payload);

            var raw = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
            {
                var reason = string.IsNullOrWhiteSpace(raw) ? resp.ReasonPhrase : raw;
                throw new HttpRequestException(reason, null, resp.StatusCode);
            }

            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(raw) ? "{}" : raw);
            var root = doc.RootElement;

            string? token = null;
            if (root.TryGetProperty("token", out var p)) token = p.GetString();
            else if (root.TryGetProperty("accessToken", out p)) token = p.GetString();
            else if (root.TryGetProperty("jwt", out p)) token = p.GetString();

            var redirect = root.TryGetProperty("redirect", out var r) ? r.GetString() : null;

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new HttpRequestException("Login response did not contain a token.", null, HttpStatusCode.BadRequest);
            }

            return new LoginResponse(token!, redirect);
        }

        // --------------------------------------------------------------------
        // Queries
        // --------------------------------------------------------------------
        public Task<List<Product>?> GetProductsAsync(System.Threading.CancellationToken ct = default) =>
            _http.GetFromJsonAsync<List<Product>>("api/products", JsonOptions, ct);

        public Task<List<Salesperson>?> GetSalespersonsAsync(System.Threading.CancellationToken ct = default) =>
            _http.GetFromJsonAsync<List<Salesperson>>("api/salespersons", JsonOptions, ct);

        public Task<List<Customer>?> GetCustomersAsync(System.Threading.CancellationToken ct = default) =>
            _http.GetFromJsonAsync<List<Customer>>("api/customers", JsonOptions, ct);

        public Task<List<Sale>?> GetSalesAsync(DateOnly? start = null, DateOnly? end = null, System.Threading.CancellationToken ct = default)
        {
            var q = new System.Collections.Generic.List<string>();
            if (start is not null) q.Add($"start={start:yyyy-MM-dd}");
            if (end is not null) q.Add($"end={end:yyyy-MM-dd}");
            var url = "api/sales" + (q.Count > 0 ? "?" + string.Join("&", q) : "");
            return _http.GetFromJsonAsync<List<Sale>>(url, JsonOptions, ct);
        }

        // --------------------------------------------------------------------
        // Products
        // --------------------------------------------------------------------
        public async Task UpdateProductAsync(Product product, System.Threading.CancellationToken ct = default)
        {
            var resp = await _http.PutAsJsonAsync($"api/products/{product.Id}", product, ct);
            resp.EnsureSuccessStatusCode();
        }

        // Create product - used by pages that will create a product when not found
        public async Task<Product> CreateProductAsync(ProductUpsertDto dto, System.Threading.CancellationToken ct = default)
        {
            // serialize DTO to JSON for logging (will follow same property names sent)
            var requestJson = JsonSerializer.Serialize(dto, JsonOptions);
            _logger.LogInformation("CreateProductAsync request JSON: {Json}", requestJson);

            var resp = await _http.PostAsJsonAsync("api/products", dto, ct);

            // If the API returns a 4xx/5xx, capture response body and log it so you can see validation errors
            if (!resp.IsSuccessStatusCode)
            {
                var raw = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("CreateProductAsync failed: {StatusCode} {Reason}. Response body: {Body}",
                    (int)resp.StatusCode, resp.ReasonPhrase, raw);

                var reason = string.IsNullOrWhiteSpace(raw) ? resp.ReasonPhrase : raw;
                throw new HttpRequestException($"Failed to create product: {(int)resp.StatusCode} {resp.StatusCode}: {reason}", null, resp.StatusCode);
            }

            var created = await resp.Content.ReadFromJsonAsync<Product>(JsonOptions, ct);
            return created ?? throw new InvalidOperationException("No product returned.");
        }

        // --------------------------------------------------------------------
        // Sales
        // --------------------------------------------------------------------
        // Add overload that accepts quantity and unitPrice so callers can pass those values.
        public async Task<Sale> CreateSaleAsync(int productId, int salespersonId, int customerId, DateOnly salesDate, System.Threading.CancellationToken ct = default)
        {
            // Keep original simple signature for backward compatibility
            var payload = new { ProductId = productId, SalespersonId = salespersonId, CustomerId = customerId, SalesDate = salesDate };
            var resp = await _http.PostAsJsonAsync("api/sales", payload, ct);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<Sale>(JsonOptions, ct))
                   ?? throw new InvalidOperationException("No sale returned.");
        }

        // New overload that matches the call site with quantity and unitPrice
        public async Task<Sale> CreateSaleAsync(int productId, int salespersonId, int customerId, DateOnly salesDate, int quantity, decimal? unitPrice, System.Threading.CancellationToken ct = default)
        {
            var payload = new
            {
                ProductId = productId,
                SalespersonId = salespersonId,
                CustomerId = customerId,
                SalesDate = salesDate,
                Quantity = quantity,
                UnitPrice = unitPrice
            };
            var resp = await _http.PostAsJsonAsync("api/sales", payload, ct);
            resp.EnsureSuccessStatusCode();
            return (await resp.Content.ReadFromJsonAsync<Sale>(JsonOptions, ct))
                   ?? throw new InvalidOperationException("No sale returned.");
        }

        // --------------------------------------------------------------------
        // Customers
        // --------------------------------------------------------------------
        // Create customer - used by pages that will create a customer when not found
        public async Task<Customer> CreateCustomerAsync(CustomerUpsertDto dto, System.Threading.CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("api/customers", dto, ct);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<Customer>(JsonOptions, ct);
            return created ?? throw new InvalidOperationException("No customer returned.");
        }

        // --------------------------------------------------------------------
        // Salesperson CRUD
        // --------------------------------------------------------------------
        public async Task<Salesperson> CreateSalespersonAsync(SalespersonUpsertDto dto, System.Threading.CancellationToken ct = default)
        {
            var resp = await _http.PostAsJsonAsync("api/salespersons", dto, ct);
            resp.EnsureSuccessStatusCode();

            var created = await resp.Content.ReadFromJsonAsync<Salesperson>(JsonOptions, ct);
            return created ?? throw new InvalidOperationException("No salesperson returned.");
        }

        public async Task UpdateSalespersonAsync(int id, SalespersonUpsertDto dto, System.Threading.CancellationToken ct = default)
        {
                var resp = await _http.PutAsJsonAsync($"api/salespersons/{id}", dto, ct);
            resp.EnsureSuccessStatusCode();
        }

        public async Task DeleteSalespersonAsync(int id, System.Threading.CancellationToken ct = default)
        {
            var resp = await _http.DeleteAsync($"api/salespersons/{id}", ct);
            resp.EnsureSuccessStatusCode();
        }
    }
}