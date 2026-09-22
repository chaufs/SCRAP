using System;

namespace SCRAP.domain.entities
{
    public class PayrollResult
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public PayType PayType { get; set; }
        public decimal PayRate { get; set; }
        public decimal PeriodBasePay { get; set; }
        public decimal PresentDays { get; set; }
        public decimal PaidLeaveDays { get; set; }
        public decimal UnpaidAbsenceDays { get; set; }
        public decimal AbsenceDeduction { get; set; }
        public decimal GrossPay { get; set; }
        public decimal EmployeeTaxRate { get; set; }
        public decimal EmployeeTax { get; set; }
        public decimal NetPay { get; set; }
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }
    }
}
