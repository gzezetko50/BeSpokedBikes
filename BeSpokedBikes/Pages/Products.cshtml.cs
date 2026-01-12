using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BeSpokedBikes.Models;
using BeSpokedBikes.Services;

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
            var product = new Product
            {
                Id = Input.Id,
                Name = Input.Name,
                ManufacturerId = Input.ManufacturerId,
                ManufacturerName = Input.Manufacturer,
                StyleId = Input.StyleId,
                StyleName = Input.Style,
                PurchasePrice = Input.PurchasePrice,
                SalePrice = Input.SalePrice,
                QtyOnHand = Input.QtyOnHand,
                CommissionPercentage = Input.CommissionPercentage,
                StatusId = Input.StatusId
            };

            try
            {
                await _apiClient.UpdateProductAsync(product);
            }
            catch (System.Net.Http.HttpRequestException ex)
            {
                // Mirror Create/Sales error handling: show API error to user and reload list
                ModelState.AddModelError(string.Empty, $"Failed to update product: {ex.Message}");
                await OnGetAsync(); // reload products for the page
                return Page();
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync()
        {
            // implement delete when API is available
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            // Build DTO that matches ApiClient.ProductUpsertDto (server expects manufacturerId non-nullable).
            var dto = new ApiClient.ProductUpsertDto(
                Input.Name?.Trim() ?? string.Empty,
                Input.ManufacturerId ?? 0,        // send 0 if unknown
                string.IsNullOrWhiteSpace(Input.Manufacturer) ? null : Input.Manufacturer.Trim(),
                Input.StyleId,
                string.IsNullOrWhiteSpace(Input.Style) ? null : Input.Style.Trim(),
                Input.PurchasePrice,
                Input.SalePrice,
                Input.QtyOnHand,
                Input.CommissionPercentage,
                Input.StatusId
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