using System;

namespace SCRAP.domain.entities
{
    public enum StorageDestructionStatus
    {
        PendingDestruction,
        Destroyed
    }

    public class StorageDestructionRecord
    {
        public int Id { get; set; }
        public int InventoryId { get; set; }
        public int TeardownBatchId { get; set; }
        public StorageDestructionStatus Status { get; set; } = StorageDestructionStatus.PendingDestruction;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Inventory? Inventory { get; set; }
        public TeardownBatch? TeardownBatch { get; set; }
    }
}