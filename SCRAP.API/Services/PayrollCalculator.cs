using SCRAP.domain.entities;

namespace SCRAP.API.Services
{
    public sealed class PayrollCalculator
    {
        public PayrollResult Calculate(
            Employee employee,
            DateTime periodStart,
            DateTime periodEnd,
            IReadOnlyCollection<AttendanceRecord> attendance,
            IReadOnlyCollection<PaidLeaveRequest> approvedLeaves)
        {
            if (periodEnd.Date < periodStart.Date)
                throw new ArgumentException("Payroll period end must be on or after the start date.");

            var start = periodStart.Date;
            var end = periodEnd.Date;
            var totalWorkdays = CountWeekdays(start, end);
            var presentDays = attendance.Count(x => x.Status == AttendanceStatus.Present && IsInPeriod(x.AttendanceDate, start, end));
            var paidLeaveDays = approvedLeaves
                .Where(x => x.Status == LeaveRequestStatus.Approved)
                .Sum(x => CountWeekdays(Max(x.StartDate.Date, start), Min(x.EndDate.Date, end)));

            var coveredDays = presentDays + paidLeaveDays;
            var unpaidAbsenceDays = Math.Max(0, totalWorkdays - coveredDays);
            var dailyRate = GetDailyRate(employee, totalWorkdays);
            var absenceDeduction = RoundCurrency(unpaidAbsenceDays * dailyRate);
            var periodBasePay = RoundCurrency(GetPeriodBasePay(employee, totalWorkdays));
            var grossPay = RoundCurrency(Math.Max(0m, periodBasePay - absenceDeduction));
            const decimal employeeTaxRate = 0.10m;
            var employeeTax = RoundCurrency(grossPay * employeeTaxRate);
            var netPay = RoundCurrency(Math.Max(0m, grossPay - employeeTax));

            return new PayrollResult
            {
                EmployeeId = employee.Id,
                EmployeeCode = employee.EmployeeCode,
                EmployeeName = string.Join(" ", new[] { employee.FirstName, employee.MiddleName, employee.LastName }
                    .Where(x => !string.IsNullOrWhiteSpace(x))),
                PayType = employee.PayType,
                PayRate = employee.PayRate,
                PeriodBasePay = periodBasePay,
                PresentDays = presentDays,
                PaidLeaveDays = paidLeaveDays,
                UnpaidAbsenceDays = unpaidAbsenceDays,
                AbsenceDeduction = absenceDeduction,
                GrossPay = grossPay,
                EmployeeTaxRate = employeeTaxRate,
                EmployeeTax = employeeTax,
                NetPay = netPay,
                PeriodStart = start,
                PeriodEnd = end
            };
        }

        private static decimal GetPeriodBasePay(Employee employee, int workdays)
        {
            return employee.PayType switch
            {
                PayType.Monthly => employee.PayRate,
                PayType.Daily => employee.PayRate * workdays,
                PayType.Hourly => employee.PayRate * workdays * 8m,
                _ => 0m
            };
        }

        private static decimal GetDailyRate(Employee employee, int workdays)
        {
            return employee.PayType switch

            {
                PayType.Monthly => workdays == 0 ? 0m : employee.PayRate / workdays,
                PayType.Daily => employee.PayRate,
                PayType.Hourly => employee.PayRate * 8m,
                _ => 0m
            };
        }

        private static int CountWeekdays(DateTime start, DateTime end)
        {
            if (end < start) return 0;
            var count = 0;
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    count++;
            }
            return count;
        }

        private static bool IsInPeriod(DateTime value, DateTime start, DateTime end) => value.Date >= start && value.Date <= end;
        private static DateTime Max(DateTime left, DateTime right) => left > right ? left : right;
        private static DateTime Min(DateTime left, DateTime right) => left < right ? left : right;
        private static decimal RoundCurrency(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
