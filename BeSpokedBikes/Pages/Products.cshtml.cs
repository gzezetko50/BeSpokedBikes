using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;
using System.Text.Json;

namespace BeSpokedBikes.Pages
{
    public class ProductsModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public ProductsModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public List<Product> Products { get; set; } = new();

        [BindProperty]
        public ProductInputModel Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var list = await _apiClient.GetProductsAsync();
                Products = list ?? new();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Products = new();
            }
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            // Build DTO-like payload that matches server expectations:
            // the server expects a non-nullable manufacturerId (send 0 when unknown)
            var product = new Product
            {
                Id = Input.Id,
                Name = Input.Name?.Trim() ?? string.Empty,
                ManufacturerId = Input.ManufacturerId ?? 0, // send 0 if unknown
                ManufacturerName = string.IsNullOrWhiteSpace(Input.Manufacturer) ? null : Input.Manufacturer.Trim(),
                StyleId = Input.StyleId ?? 0, // send 0 if unknown
                StyleName = string.IsNullOrWhiteSpace(Input.Style) ? null : Input.Style.Trim(),
                PurchasePrice = Input.PurchasePrice,
                SalePrice = Input.SalePrice,
                QtyOnHand = Input.QtyOnHand,
                CommissionPercentage = Input.CommissionPercentage,
                StatusId = Input.StatusId ?? 0 // ensure an integer is sent (avoid JSON null causing server-side conversion error)
            };

            try
            {
                await _apiClient.UpdateProductAsync(product);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Try to extract a structured API message from the exception (if ApiClient attached it)
                string userMessage = ex.Message;

                if (ex.Data.Contains("ApiResponseBody") && ex.Data["ApiResponseBody"] is string body)
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(body);
                        var root = doc.RootElement;

                        // Prefer "message" then "errors" then fallback to whole body
                        if (root.TryGetProperty("message", out var msgProp) && msgProp.ValueKind == JsonValueKind.String)
                        {
                            var apiMsg = msgProp.GetString();
                            var detailsText = string.Empty;

                            if (root.TryGetProperty("details", out var detailsProp) && detailsProp.ValueKind == JsonValueKind.String)
                            {
                                var d = detailsProp.GetString();
                                if (!string.IsNullOrWhiteSpace(d))
                                {
                                    detailsText = $" Details: {d}";
                                }
                            }

                            userMessage = apiMsg + detailsText;
                        }
                        else if (root.TryGetProperty("errors", out var errorsProp))
                        {
                            // Flatten validation errors for display
                            if (errorsProp.ValueKind == JsonValueKind.Object)
                            {
                                var sb = new System.Text.StringBuilder();
                                foreach (var p in errorsProp.EnumerateObject())
                                {
                                    sb.Append(p.Name);
                                    sb.Append(": ");
                                    if (p.Value.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var v in p.Value.EnumerateArray())
                                        {
                                            if (v.ValueKind == JsonValueKind.String)
                                            {
                                                sb.Append(v.GetString());
                                                sb.Append(" ");
                                            }
                                        }
                                    }
                                }
                                var errorsText = sb.ToString().Trim();
                                if (!string.IsNullOrEmpty(errorsText))
                                {
                                    userMessage = errorsText;
                                }
                            }
                            else
                            {
                                userMessage = body;
                            }
                        }
                        else
                        {
                            // fallback to root-level string content or full body
                            userMessage = body;
                        }
                    }
                    catch (JsonException)
                    {
                        // if parsing fails, keep raw body
                        userMessage = body;
                    }
                }

                // Mirror Create/Sales error handling: show API error to user and reload list
                ModelState.AddModelError(string.Empty, $"Failed to update product: {userMessage}");
                await OnGetAsync(); // reload products for the page
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // implement delete when API is available
            await Task.CompletedTask;
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Build DTO that matches ApiClient.ProductUpsertDto (server expects manufacturerId non-nullable).
            var dto = new ApiClient.ProductUpsertDto(
                Input.Name?.Trim() ?? string.Empty,
                Input.ManufacturerId ?? 0,        // send 0 if unknown
                string.IsNullOrWhiteSpace(Input.Manufacturer) ? null : Input.Manufacturer.Trim(),
                Input.StyleId ?? 0,               // send 0 if unknown
                string.IsNullOrWhiteSpace(Input.Style) ? null : Input.Style.Trim(),
                Input.PurchasePrice,
                Input.SalePrice,
                Input.QtyOnHand,
                Input.CommissionPercentage,
                Input.StatusId ?? 0               // ensure an integer is sent (avoid JSON null causing server-side conversion error)
            );

            try
            {
                var created = await _apiClient.CreateProductAsync(dto);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Mirror Sales page behavior: show API error to user and reload list for the page
                ModelState.AddModelError(string.Empty, $"Failed to create product: {ex.Message}");
                await OnGetAsync(); // reload products for the page
                return Page();
            }

            return RedirectToPage();
        }
    }

    public class ProductInputModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        // what you type into the two textboxes in the modal
        public string Manufacturer { get; set; } = string.Empty;
        public string Style { get; set; } = string.Empty;

        // ids that back those names (shown in the grid)
        public int? ManufacturerId { get; set; }
        public int? StyleId { get; set; }

        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public int QtyOnHand { get; set; }
        public decimal CommissionPercentage { get; set; }
        public int? StatusId { get; set; }
    }
}