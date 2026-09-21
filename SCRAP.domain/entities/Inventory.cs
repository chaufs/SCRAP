using System;

namespace SCRAP.domain.entities
{
    public enum InventoryStatus
    {
        InStock,
        InTeardown,
        Disposed
    }

    public class Inventory
    {
        public int Id { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public int DeviceCategoryId { get; set; }
        public string SerialNumber { get; set; } = string.Empty;
        public InventoryStatus Status { get; set; }
        public DateTime DateReceived { get; set; }
        public string? Notes { get; set; }
        public bool HasStorageDevice { get; set; }   // ← add this

        // Navigation
        public DeviceCategory? DeviceCategory { get; set; }
    }
}