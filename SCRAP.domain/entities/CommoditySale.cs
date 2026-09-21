using System;

namespace SCRAP.domain.entities
{
    public class CommoditySale
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public decimal QuantityKg { get; set; }
        public decimal PricePerKg { get; set; }
        public decimal TotalAmount { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public string? Notes { get; set; }
    }
}