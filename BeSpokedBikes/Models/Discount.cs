using System;

namespace BeSpokedBikes.Models
{
    public class Discount
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public DateOnly BeginDate { get; set; }
        public DateOnly EndDate { get; set; }
        public decimal DiscountPercentage { get; set; }

        public Product? Product { get; set; }
    }
}