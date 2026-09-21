using System;

namespace SCRAP.domain.entities
{
    public enum EmploymentStatus
    {
        Active,
        OnLeave,
        Resigned,
        Terminated
    }

    public enum PayType
    {
        Hourly,
        Daily,
        Monthly
    }

    public class Employee
    {
        public int Id { get; set; }

        // Identity
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        // Contact
        public string? ContactNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Address { get; set; }

        // Job
        public string Position { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public DateTime DateHired { get; set; }
        public EmploymentStatus Status { get; set; } = EmploymentStatus.Active;

        // Pay — Finances will read these later
        public PayType PayType { get; set; } = PayType.Monthly;
        public decimal PayRate { get; set; }

        // Optional link to a login account (null = employee with no system access)
        public int? UserId { get; set; }

        public string? Notes { get; set; }
    }
}