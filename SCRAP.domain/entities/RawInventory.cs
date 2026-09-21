using System;

namespace SCRAP.domain.entities
{
    public class RawInventory
    {
        public int Id { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        // Current total weight in kilograms
        public decimal CurrentTotalWeightKg { get; set; }
    }
}
