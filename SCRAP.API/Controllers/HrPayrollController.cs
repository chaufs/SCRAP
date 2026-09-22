using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.API.Services;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/hr")]
    public class HrPayrollController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        private readonly PayrollCalculator _payroll;

        public HrPayrollController(MasterErpDbContext db, PayrollCalculator payroll)
        {
            _db = db;
            _payroll = payroll;
        }

        [HttpGet("attendance")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetAttendance(DateTime? date = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            return Ok(await _db.AttendanceRecords
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.AttendanceDate == selectedDate)
                .OrderBy(x => x.Employee!.LastName)
                .ToListAsync());
        }

        [HttpPost("attendance")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<AttendanceRecord>> MarkAttendance(MarkAttendanceRequest request)
        {
            var date = request.AttendanceDate.Date;
            if (date > DateTime.Today)
                return BadRequest("Attendance cannot be marked for a future date.");

            var employee = await _db.Set<Employee>().FindAsync(request.EmployeeId);
            if (employee is null) return NotFound("Employee not found.");
            if (employee.Status != EmploymentStatus.Active)
                return BadRequest("Only active employees can be marked for attendance.");

            var record = await _db.AttendanceRecords
                .SingleOrDefaultAsync(x => x.EmployeeId == request.EmployeeId && x.AttendanceDate == date);

            if (record is null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = request.EmployeeId,
                    AttendanceDate = date
                };
                _db.AttendanceRecords.Add(record);
            }

            record.Status = request.Status;
            record.Notes = request.Notes;
            record.MarkedAtUtc = DateTime.UtcNow;
            record.MarkedByUserId = GetCurrentUserId();

            await _db.SaveChangesAsync();
            return Ok(record);
        }

        [HttpGet("leave-balances")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<IEnumerable<EmployeeLeaveBalance>>> GetLeaveBalances(int? year = null)
        {
            var leaveYear = year ?? DateTime.Today.Year;
            return Ok(await _db.EmployeeLeaveBalances
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.LeaveYear == leaveYear)
                .OrderBy(x => x.Employee!.LastName)
                .ToListAsync());
        }

        [HttpPost("leave-balances")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<EmployeeLeaveBalance>> SetLeaveBalance(SetLeaveBalanceRequest request)
        {
            if (request.MaximumPaidLeaveDays < 0)
                return BadRequest("Maximum paid leave cannot be negative.");

            if (!await _db.Set<Employee>().AnyAsync(x => x.Id == request.EmployeeId))
                return NotFound("Employee not found.");

            var balance = await _db.EmployeeLeaveBalances.SingleOrDefaultAsync(x =>
                x.EmployeeId == request.EmployeeId && x.LeaveYear == request.LeaveYear);

            if (balance is null)
            {
                balance = new EmployeeLeaveBalance
                {
                    EmployeeId = request.EmployeeId,
                    LeaveYear = request.LeaveYear,
                    MaximumPaidLeaveDays = request.MaximumPaidLeaveDays
                };
                _db.EmployeeLeaveBalances.Add(balance);
            }
            else
            {
                if (request.MaximumPaidLeaveDays < balance.UsedPaidLeaveDays)
                    return BadRequest("Maximum paid leave cannot be less than leave already used.");
                balance.MaximumPaidLeaveDays = request.MaximumPaidLeaveDays;
            }

            await _db.SaveChangesAsync();
            return Ok(balance);
        }

        [HttpGet("leave-requests")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<PaidLeaveRequest>>> GetLeaveRequests(LeaveRequestStatus? status = null)
        {
            var query = _db.PaidLeaveRequests.AsNoTracking().Include(x => x.Employee).AsQueryable();
            if (status is not null) query = query.Where(x => x.Status == status.Value);
            return Ok(await query.OrderBy(x => x.StartDate).ToListAsync());
        }

        [HttpPost("leave-requests")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<PaidLeaveRequest>> RequestPaidLeave(CreateLeaveRequest request)
        {
            if (request.EndDate.Date < request.StartDate.Date)
                return BadRequest("Leave end date cannot be before the start date.");

            if (!await _db.Set<Employee>().AnyAsync(x => x.Id == request.EmployeeId))
                return NotFound("Employee not found.");

            var days = CountWeekdays(request.StartDate.Date, request.EndDate.Date);
            if (days <= 0) return BadRequest("The selected leave period contains no weekdays.");

            var year = request.StartDate.Year;
            var balance = await GetOrCreateBalance(request.EmployeeId, year);
            var pendingOrApproved = await _db.PaidLeaveRequests
                .Where(x => x.EmployeeId == request.EmployeeId && x.StartDate.Year == year &&
                            (x.Status == LeaveRequestStatus.Pending || x.Status == LeaveRequestStatus.Approved))
                .SumAsync(x => (decimal?)x.DaysRequested) ?? 0m;

            if (pendingOrApproved + days > balance.MaximumPaidLeaveDays)
                return BadRequest($"Paid leave limit exceeded. Remaining available days: {Math.Max(0m, balance.MaximumPaidLeaveDays - pendingOrApproved):0.##}.");

            var leave = new PaidLeaveRequest
            {
                EmployeeId = request.EmployeeId,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                DaysRequested = days,
                Reason = request.Reason.Trim(),
                RequestedByUserId = GetCurrentUserId(),
                Status = LeaveRequestStatus.Pending
            };
            _db.PaidLeaveRequests.Add(leave);
            await _db.SaveChangesAsync();
            return Ok(leave);
        }

        [HttpPost("leave-requests/{id:int}/approve")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<PaidLeaveRequest>> ApproveLeave(int id)
        {
            var leave = await _db.PaidLeaveRequests.FindAsync(id);
            if (leave is null) return NotFound();
            if (leave.Status != LeaveRequestStatus.Pending) return BadRequest("Only pending leave requests can be approved.");

            var balance = await GetOrCreateBalance(leave.EmployeeId, leave.StartDate.Year);
            var used = await _db.PaidLeaveRequests
                .Where(x => x.EmployeeId == leave.EmployeeId && x.Id != leave.Id && x.StartDate.Year == leave.StartDate.Year && x.Status == LeaveRequestStatus.Approved)
                .SumAsync(x => (decimal?)x.DaysRequested) ?? 0m;

            if (used + leave.DaysRequested > balance.MaximumPaidLeaveDays)
                return BadRequest("Approving this leave would exceed the employee's paid leave limit.");

            leave.Status = LeaveRequestStatus.Approved;
            leave.ApprovedByUserId = GetCurrentUserId();
            leave.ApprovedAtUtc = DateTime.UtcNow;
            balance.UsedPaidLeaveDays = used + leave.DaysRequested;

            await _db.SaveChangesAsync();
            return Ok(leave);
        }

        [HttpPost("leave-requests/{id:int}/reject")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<PaidLeaveRequest>> RejectLeave(int id)
        {
            var leave = await _db.PaidLeaveRequests.FindAsync(id);
            if (leave is null) return NotFound();
            if (leave.Status != LeaveRequestStatus.Pending) return BadRequest("Only pending leave requests can be rejected.");
            leave.Status = LeaveRequestStatus.Rejected;
            leave.ApprovedByUserId = GetCurrentUserId();
            leave.ApprovedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(leave);
        }

        [HttpGet("payroll")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<PayrollResult>>> GeneratePayroll(DateTime periodStart, DateTime periodEnd)
        {
            if (periodEnd.Date < periodStart.Date)
                return BadRequest("Payroll period end cannot be before the start date.");

            var employees = await _db.Set<Employee>()
                .AsNoTracking()
                .Where(x => x.Status == EmploymentStatus.Active)
                .OrderBy(x => x.LastName)
                .ToListAsync();
            var start = periodStart.Date;
            var end = periodEnd.Date;
            var employeeIds = employees.Select(x => x.Id).ToList();
            var attendance = await _db.AttendanceRecords.AsNoTracking()
                .Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate >= start && x.AttendanceDate <= end)
                .ToListAsync();
            var leaves = await _db.PaidLeaveRequests.AsNoTracking()
                .Where(x => employeeIds.Contains(x.EmployeeId) && x.Status == LeaveRequestStatus.Approved && x.EndDate >= start && x.StartDate <= end)
                .ToListAsync();

            var results = employees.Select(employee => _payroll.Calculate(
                employee,
                start,
                end,
                attendance.Where(x => x.EmployeeId == employee.Id).ToList(),
                leaves.Where(x => x.EmployeeId == employee.Id).ToList())).ToList();

            foreach (var result in results.Where(x => x.EmployeeTax > 0))
            {
                var sourceReference = $"PayrollTax:{result.EmployeeId}:{start:yyyyMMdd}:{end:yyyyMMdd}";
                if (!await _db.CompanyFinanceTransactions.AnyAsync(x => x.SourceReference == sourceReference))
                {
                    _db.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
                    {
                        Type = FinanceTransactionType.Deduction,
                        Category = "Employee Payroll Tax",
                        Amount = result.EmployeeTax,
                        TransactionDate = end,
                        Description = $"Payroll tax for {result.EmployeeName} ({start:yyyy-MM-dd} to {end:yyyy-MM-dd})",
                        SourceReference = sourceReference,
                        RecordedByUserId = GetCurrentUserId()
                    });
                }
            }

            await _db.SaveChangesAsync();

            return Ok(results);
        }

        private async Task<EmployeeLeaveBalance> GetOrCreateBalance(int employeeId, int year)
        {
            var balance = await _db.EmployeeLeaveBalances.SingleOrDefaultAsync(x => x.EmployeeId == employeeId && x.LeaveYear == year);
            if (balance is not null) return balance;

            balance = new EmployeeLeaveBalance { EmployeeId = employeeId, LeaveYear = year };
            _db.EmployeeLeaveBalances.Add(balance);
            await _db.SaveChangesAsync();
            return balance;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private static int CountWeekdays(DateTime start, DateTime end)
        {
            var count = 0;
            for (var date = start; date <= end; date = date.AddDays(1))
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count++;
            return count;
        }

        public sealed class MarkAttendanceRequest
        {
            public int EmployeeId { get; set; }
            public DateTime AttendanceDate { get; set; } = DateTime.Today;
            public AttendanceStatus Status { get; set; }
            public string? Notes { get; set; }
        }

        public sealed class SetLeaveBalanceRequest
        {
            public int EmployeeId { get; set; }
            public int LeaveYear { get; set; } = DateTime.Today.Year;
            public decimal MaximumPaidLeaveDays { get; set; } = 12m;
        }

        public sealed class CreateLeaveRequest
        {
            public int EmployeeId { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
