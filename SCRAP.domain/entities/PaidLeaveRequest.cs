using System;

namespace SCRAP.domain.entities
{
    public enum LeaveRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public class PaidLeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal DaysRequested { get; set; }
        public LeaveRequestStatus Status { get; set; } = LeaveRequestStatus.Pending;
        public string Reason { get; set; } = string.Empty;
        public int? RequestedByUserId { get; set; }
        public int? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        public Employee? Employee { get; set; }
    }
}
