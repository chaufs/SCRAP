using System;

namespace SCRAP.domain.entities
{
    public enum AttendanceStatus
    {
        Present,
        PaidLeave,
        Absent
    }

    public class AttendanceRecord
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public DateTime AttendanceDate { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
        public int? MarkedByUserId { get; set; }
        public DateTime MarkedAtUtc { get; set; } = DateTime.UtcNow;

        public Employee? Employee { get; set; }
    }
}
