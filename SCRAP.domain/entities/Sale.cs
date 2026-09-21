using System;

namespace SCRAP.domain.entities
{
    public class Sale
    {
        public int Id { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public DateTime SaleDate { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
    }
}