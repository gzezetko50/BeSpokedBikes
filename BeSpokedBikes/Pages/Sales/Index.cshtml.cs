using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;

namespace BeSpokedBikes.Pages.Sales
{
    public partial class IndexModel : PageModel
    {
        private readonly ApiClient _apiClient;

        public IndexModel(ApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public List<Sale> Sales { get; set; } = new List<Sale>();

        // Input model bound to modal form
        public class InputModel
        {
            public int SaleId { get; set; }
            public int ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int SalespersonId { get; set; }
            public string SalespersonName { get; set; } = string.Empty;
            public int CustomerId { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public DateOnly? SalesDate { get; set; }
            public int Quantity { get; set; } = 1;
            public decimal? UnitPrice { get; set; }
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                var list = await _apiClient.GetSalesAsync();
                Sales = list ?? new List<Sale>();
            }
            catch (System.Net.Http.HttpRequestException)
            {
                Sales = new List<Sale>();
            }
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Resolve or create product
            var productId = Input.ProductId;
            if (productId == 0 && !string.IsNullOrWhiteSpace(Input.ProductName))
            {
                var products = await _apiClient.GetProductsAsync();
                var found = products?.FirstOrDefault(p => string.Equals(p.Name?.Trim(), Input.ProductName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (found is not null) productId = found.Id;
                else
                {
                    // Build a ProductUpsertDto and post it. Use concrete numbers (0) for integer fields the API expects
                    var dto = new ApiClient.ProductUpsertDto(
                        Input.ProductName.Trim(),
                        0,                  // manufacturerId: send 0 when unknown if server expects non-nullable int
                        null,               // manufacturerName
                        null,               // styleId
                        null,               // styleName
                        0m,                 // purchasePrice
                        Input.UnitPrice ?? 0m, // salePrice
                        0,                  // qtyOnHand
                        0m,                 // commissionPercentage
                        0                   // statusId (send 0 when server expects default)
                    );

                    try
                    {
                        var createdProd = await _apiClient.CreateProductAsync(dto);
                        productId = createdProd.Id;
                    }
                    catch (System.Net.Http.HttpRequestException ex)
                    {
                        ModelState.AddModelError(string.Empty, $"Failed to create product: {ex.Message}");
                        await OnGetAsync(); // reload sales for page
                        return Page();
                    }
                }
            }

            // Resolve or create salesperson
            var salespersonId = Input.SalespersonId;
            if (salespersonId == 0 && !string.IsNullOrWhiteSpace(Input.SalespersonName))
            {
                var salespersons = await _apiClient.GetSalespersonsAsync();
                var parts = Input.SalespersonName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var first = parts.Length > 0 ? parts[0] : string.Empty;
                var last = parts.Length > 1 ? parts[1] : string.Empty;
                var found = salespersons?.FirstOrDefault(sp =>
                    string.Equals(sp.FirstName?.Trim(), first, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(sp.LastName?.Trim(), last, StringComparison.OrdinalIgnoreCase));
                if (found is not null) salespersonId = found.Id;
                else
                {
                    var dto = new ApiClient.SalespersonUpsertDto(
                        first,
                        last,
                        DateOnly.FromDateTime(DateTime.UtcNow),
                        null,
                        null,
                        null,
                        null,
                        null
                    );
                    var createdSp = await _apiClient.CreateSalespersonAsync(dto);
                    salespersonId = createdSp.Id;
                }
            }

            // Resolve or create customer
            var customerId = Input.CustomerId;
            if (customerId == 0 && !string.IsNullOrWhiteSpace(Input.CustomerName))
            {
                var customers = await _apiClient.GetCustomersAsync();
                var name = Input.CustomerName.Trim();
                var found = customers?.FirstOrDefault(c =>
                    string.Equals($"{c.FirstName} {c.LastName}".Trim(), name, StringComparison.OrdinalIgnoreCase));
                if (found is not null) customerId = found.Id;
                else
                {
                    var parts = name.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    var first = parts.Length > 0 ? parts[0] : string.Empty;
                    var last = parts.Length > 1 ? parts[1] : string.Empty;
                    var custDto = new ApiClient.CustomerUpsertDto(
                        first,
                        last,
                        string.Empty,
                        Input.SalesDate,
                        null,
                        null,
                        null
                    );
                    var createdCust = await _apiClient.CreateCustomerAsync(custDto);
                    customerId = createdCust.Id;
                }
            }

            // Ensure we have IDs
            if (productId == 0 || salespersonId == 0 || customerId == 0)
            {
                ModelState.AddModelError(string.Empty, "Unable to resolve Product, Salesperson or Customer.");
                await OnGetAsync(); // reload sales for page
                return Page();
            }

            // Create sale
            try
            {
                var salesDate = Input.SalesDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
                var sale = await _apiClient.CreateSaleAsync(productId, salespersonId, customerId, salesDate, Input.Quantity, Input.UnitPrice);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                ModelState.AddModelError(string.Empty, $"Failed to create sale: {ex.Message}");
                await OnGetAsync();
                return Page();
            }

            return RedirectToPage();
        }
    }
}