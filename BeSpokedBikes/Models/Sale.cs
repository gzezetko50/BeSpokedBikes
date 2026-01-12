using System;
using System.Text.Json.Serialization;

namespace BeSpokedBikes.Models
{
    public class Sale
    {
        [JsonPropertyName("saleId")]
        public int SaleId { get; set; }

        // New ID-based fields
        [JsonPropertyName("productId")]
        public int ProductId { get; set; }

        [JsonPropertyName("salespersonId")]
        public int SalespersonId { get; set; }

        [JsonPropertyName("customerId")]
        public int CustomerId { get; set; }

        // Existing display fields
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("salespersonFirstName")]
        public string SalespersonFirstName { get; set; } = string.Empty;

        [JsonPropertyName("salespersonLastName")]
        public string SalespersonLastName { get; set; } = string.Empty;

        [JsonPropertyName("customerFirstName")]
        public string CustomerFirstName { get; set; } = string.Empty;

        [JsonPropertyName("customerLastName")]
        public string CustomerLastName { get; set; } = string.Empty;

        // If API returns timestamp, your DateOnly converter in ApiClient will handle common formats
        [JsonPropertyName("salesDate")]
        public DateOnly SalesDate { get; set; }

        // Quantity / pricing
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("unitPrice")]
        public decimal UnitPrice { get; set; }

        // Keep existing SalePrice, but align to TotalPrice naming
        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        // You can keep SalePrice mapped as an alias if API still returns salePrice
        [JsonPropertyName("salePrice")]
        public decimal SalePrice { get; set; }

        // Commission
        [JsonPropertyName("commissionAmount")]
        public decimal CommissionAmount { get; set; }

        [JsonPropertyName("commission")]
        public decimal Commission { get; set; }

        // Status / audit
        [JsonPropertyName("statusId")]
        public int StatusId { get; set; }

        [JsonPropertyName("createdDate")]
        public DateTime CreatedDate { get; set; }

        [JsonPropertyName("modifiedDate")]
        public DateTime? ModifiedDate { get; set; }
    }
}