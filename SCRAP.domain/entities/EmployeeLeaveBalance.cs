using System;

namespace SCRAP.domain.entities
{
    public class EmployeeLeaveBalance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public int LeaveYear { get; set; }
        public decimal MaximumPaidLeaveDays { get; set; } = 12m;
        public decimal UsedPaidLeaveDays { get; set; }

        public Employee? Employee { get; set; }
    }
}
