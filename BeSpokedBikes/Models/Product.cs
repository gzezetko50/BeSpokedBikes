using System.Text.Json.Serialization;

namespace BeSpokedBikes.Models
{
    public class Product
    {
        [JsonPropertyName("productId")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        // Manufacturer
        [JsonPropertyName("manufacturerId")]
        public int? ManufacturerId { get; set; }

        [JsonPropertyName("manufacturerName")]
        public string? ManufacturerName { get; set; }

        // Style
        [JsonPropertyName("styleId")]
        public int? StyleId { get; set; }

        [JsonPropertyName("styleName")]
        public string? StyleName { get; set; }

        // Prices
        [JsonPropertyName("purchasePrice")]
        public decimal PurchasePrice { get; set; }

        [JsonPropertyName("salePrice")]
        public decimal SalePrice { get; set; }

        // Inventory
        [JsonPropertyName("qtyOnHand")]
        public int QtyOnHand { get; set; }

        // Commission
        [JsonPropertyName("commissionPercentage")]
        public decimal CommissionPercentage { get; set; }

        // Status
        [JsonPropertyName("statusId")]
        public int? StatusId { get; set; }
    }
}