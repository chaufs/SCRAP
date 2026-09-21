using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities
{
    public class Teardown
    {
        public int Id { get; set; }
        public int InventoryItemId { get; set; }
        public DateTime StartedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? MaterialsRecovered { get; set; }
        public string? PerformedBy { get; set; }
        public string? Notes { get; set; }

        public Inventory? InventoryItem { get; set; }
    }
}