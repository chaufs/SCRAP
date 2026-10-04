using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities
{
    public enum SyncState
    {
        Offline,
        Synced,
        Syncing,
        PendingSync,
        Error
    }

    public class CloudSyncStatus
    {
        public bool IsOnline { get; set; }
        public bool IsSyncing { get; set; }
        public SyncState State { get; set; }
        public string StatusText => State switch
        {
            SyncState.Synced => "Cloud Synced",
            SyncState.Syncing => "Syncing...",
            SyncState.PendingSync => "Sync Pending",
            SyncState.Offline => "Offline (Local Only)",
            SyncState.Error => "Sync Error",
            _ => "Unknown"
        };
        public DateTime? LastSyncUtc { get; set; }
        public int SyncedEntitiesCount { get; set; }
        public string? CloudMasterHost { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string> TenantStatuses { get; set; } = new();
    }

    public class CloudSyncResult
    {
        public bool Success { get; set; }
        public int RecordsPushed { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SyncTimestamp { get; set; } = DateTime.UtcNow;
        public List<string> Details { get; set; } = new();
    }
}
