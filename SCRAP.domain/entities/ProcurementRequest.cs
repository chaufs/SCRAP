using System;

namespace SCRAP.domain.entities
{
    public enum ProcurementStatus
    {
        PendingApproval = 0,
        Approved = 1,
        Rejected = 2,
        Completed = 3
    }

    public class ProcurementRequest
    {
        public int Id { get; set; }

        // Supplier & Device details
        public string SupplierCompany { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public int DeviceCategoryId { get; set; }
        public int Quantity { get; set; } = 1;
        public decimal TotalCost { get; set; }
        public decimal CostPerDevice { get; set; }
        public bool HasStorageDevice { get; set; }
        public string? SerialNumber { get; set; }
        public string? BatchCode { get; set; }
        public string? Notes { get; set; }

        public ProcurementStatus Status { get; set; } = ProcurementStatus.PendingApproval;

        // Sales Staff requester
        public int? RequestedByUserId { get; set; }
        public string? RequestedByUserName { get; set; }
        public string? RequestedByFullName { get; set; }
        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

        // Manager reviewer
        public int? ReviewedByUserId { get; set; }
        public string? ReviewedByUserName { get; set; }
        public string? ReviewedByFullName { get; set; }
        public DateTime? ReviewedAtUtc { get; set; }
        public string? RejectionReason { get; set; }

        // Tech Staff assignee
        public int? AssignedTechStaffUserId { get; set; }
        public string? AssignedTechStaffUserName { get; set; }
        public string? AssignedTechStaffFullName { get; set; }
        public DateTime? AssignedAtUtc { get; set; }

        // Tech Staff completion / arrival into inventory
        public DateTime? CompletedAtUtc { get; set; }
        public string? CompletedByUserName { get; set; }

        // Branch
        public int BranchId { get; set; }

        // Navigation
        public DeviceCategory? DeviceCategory { get; set; }
        public Branch? Branch { get; set; }
    }
}
