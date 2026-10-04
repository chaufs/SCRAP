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
        private readonly TenantErpDbContext _db;
        private readonly PayrollCalculator _payroll;

        public HrPayrollController(TenantErpDbContext db, PayrollCalculator payroll)
        {
            _db = db;
            _payroll = payroll;
        }

        private int? CallerBranchId()
        {
            var val = User.FindFirst("BranchId")?.Value;
            return int.TryParse(val, out var id) && id > 0 ? id : (int?)null;
        }

        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet("daily-roster")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult> GetDailyRoster([FromQuery] DateTime? date = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var callerBranchId = CallerBranchId();

            var empQuery = _db.Set<Employee>().AsNoTracking()
                .Where(x => x.Status == EmploymentStatus.Active);

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                empQuery = empQuery.Where(x => x.BranchId == callerBranchId.Value);
            }

            var employees = await empQuery
                .OrderBy(x => x.LastName).ThenBy(x => x.FirstName)
                .ToListAsync();

            var employeeIds = employees.Select(x => x.Id).ToList();
            var attendanceRecords = await _db.AttendanceRecords.AsNoTracking()
                .Where(x => employeeIds.Contains(x.EmployeeId) && x.AttendanceDate == selectedDate)
                .ToDictionaryAsync(x => x.EmployeeId);

            var roster = employees.Select(e =>
            {
                attendanceRecords.TryGetValue(e.Id, out var att);
                return new
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = $"{e.LastName}, {e.FirstName}" + (!string.IsNullOrWhiteSpace(e.MiddleName) ? $" {e.MiddleName[0]}." : ""),
                    Department = e.Department,
                    Position = e.Position,
                    PayType = e.PayType.ToString(),
                    PayRate = e.PayRate,
                    BranchId = e.BranchId,
                    Status = att != null ? att.Status.ToString() : "Unmarked",
                    MarkedAt = att?.MarkedAtUtc,
                    Notes = att?.Notes ?? ""
                };
            }).ToList();

            return Ok(roster);
        }

        [HttpGet("attendance-summary")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult> GetAttendanceSummary([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var start = (startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var end = (endDate ?? DateTime.Today).Date;
            if (end < start) return BadRequest("End date cannot be before start date.");

            var callerBranchId = CallerBranchId();
            var empQuery = _db.Set<Employee>().AsNoTracking()
                .Where(x => x.Status == EmploymentStatus.Active);

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                empQuery = empQuery.Where(x => x.BranchId == callerBranchId.Value);
            }

            var employees = await empQuery.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync();
            var empIds = employees.Select(x => x.Id).ToList();

            var records = await _db.AttendanceRecords.AsNoTracking()
                .Where(x => empIds.Contains(x.EmployeeId) && x.AttendanceDate >= start && x.AttendanceDate <= end)
                .ToListAsync();

            var leaves = await _db.PaidLeaveRequests.AsNoTracking()
                .Where(x => empIds.Contains(x.EmployeeId) && x.Status == LeaveRequestStatus.Approved && x.EndDate >= start && x.StartDate <= end)
                .ToListAsync();

            int workDays = CountWeekdays(start, end);

            var summary = employees.Select(e =>
            {
                var empRecs = records.Where(r => r.EmployeeId == e.Id).ToList();
                int presentCount = empRecs.Count(r => r.Status == AttendanceStatus.Present);
                int absentCount = empRecs.Count(r => r.Status == AttendanceStatus.Absent);
                decimal leaveDays = leaves
                    .Where(l => l.EmployeeId == e.Id)
                    .Sum(l => CountWeekdays(Max(l.StartDate.Date, start), Min(l.EndDate.Date, end)));

                decimal rate = (presentCount + leaveDays) > 0 && workDays > 0
                    ? Math.Min(100m, Math.Round(((presentCount + leaveDays) / workDays) * 100m, 1))
                    : 0m;

                return new
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = $"{e.LastName}, {e.FirstName}" + (!string.IsNullOrWhiteSpace(e.MiddleName) ? $" {e.MiddleName[0]}." : ""),
                    Department = e.Department,
                    Position = e.Position,
                    WorkingDays = workDays,
                    PresentDays = presentCount,
                    AbsentDays = absentCount,
                    LeaveDays = leaveDays,
                    AttendanceRate = $"{rate:F1}%",
                    RateNumeric = rate
                };
            }).ToList();

            return Ok(summary);
        }

        [HttpGet("staff-history")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult> GetStaffHistory([FromQuery] int employeeId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var emp = await _db.Set<Employee>().FindAsync(employeeId);
            if (emp == null) return NotFound("Employee not found.");

            var callerBranchId = CallerBranchId();
            if (!IsAdmin() && callerBranchId.HasValue && emp.BranchId != callerBranchId.Value)
            {
                return Forbid();
            }

            var query = _db.AttendanceRecords.AsNoTracking()
                .Where(a => a.EmployeeId == employeeId);

            if (startDate.HasValue) query = query.Where(a => a.AttendanceDate >= startDate.Value.Date);
            if (endDate.HasValue) query = query.Where(a => a.AttendanceDate <= endDate.Value.Date);

            var logs = await query
                .OrderByDescending(a => a.AttendanceDate)
                .Select(a => new
                {
                    a.Id,
                    Date = a.AttendanceDate.ToString("yyyy-MM-dd"),
                    DayOfWeek = a.AttendanceDate.DayOfWeek.ToString(),
                    Status = a.Status.ToString(),
                    MarkedAt = a.MarkedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                    Notes = a.Notes ?? ""
                })
                .ToListAsync();

            return Ok(new
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                EmployeeName = $"{emp.LastName}, {emp.FirstName}" + (!string.IsNullOrWhiteSpace(emp.MiddleName) ? $" {emp.MiddleName[0]}." : ""),
                Department = emp.Department,
                Position = emp.Position,
                BranchId = emp.BranchId,
                History = logs
            });
        }

        [HttpGet("attendance")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<AttendanceRecord>>> GetAttendance(DateTime? date = null)
        {
            var selectedDate = (date ?? DateTime.Today).Date;
            var callerBranchId = CallerBranchId();
            var q = _db.AttendanceRecords
                .AsNoTracking()
                .Include(x => x.Employee)
                .Where(x => x.AttendanceDate == selectedDate);

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                q = q.Where(x => x.Employee!.BranchId == callerBranchId.Value);
            }

            return Ok(await q.OrderBy(x => x.Employee!.LastName).ToListAsync());
        }

        [HttpPost("attendance/bulk")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult> BulkMarkAttendance([FromBody] BulkAttendanceRequest request)
        {
            if (request == null || request.EmployeeIds == null || request.EmployeeIds.Count == 0)
                return BadRequest("No employees specified.");

            var date = request.AttendanceDate.Date;
            if (date > DateTime.Today)
                return BadRequest("Attendance cannot be marked for a future date.");

            var callerBranchId = CallerBranchId();
            var empQuery = _db.Set<Employee>().Where(e => request.EmployeeIds.Contains(e.Id) && e.Status == EmploymentStatus.Active);
            if (!IsAdmin() && callerBranchId.HasValue)
            {
                empQuery = empQuery.Where(e => e.BranchId == callerBranchId.Value);
            }

            var employees = await empQuery.ToListAsync();
            var empIds = employees.Select(e => e.Id).ToList();

            var existing = await _db.AttendanceRecords
                .Where(a => empIds.Contains(a.EmployeeId) && a.AttendanceDate == date)
                .ToListAsync();

            var existingDict = existing.ToDictionary(a => a.EmployeeId);
            var now = DateTime.UtcNow;
            var userId = GetCurrentUserId();

            foreach (var emp in employees)
            {
                if (existingDict.TryGetValue(emp.Id, out var rec))
                {
                    rec.Status = request.Status;
                    rec.MarkedAtUtc = now;
                    rec.MarkedByUserId = userId;
                }
                else
                {
                    _db.AttendanceRecords.Add(new AttendanceRecord
                    {
                        EmployeeId = emp.Id,
                        AttendanceDate = date,
                        Status = request.Status,
                        MarkedAtUtc = now,
                        MarkedByUserId = userId
                    });
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new { count = employees.Count, status = request.Status.ToString() });
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

        [HttpGet("my-portal")]
        [Authorize]
        public async Task<ActionResult> GetMyPortal()
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized("No user context.");

            var emp = await _db.Set<Employee>().AsNoTracking()
                .Include(e => e.Branch)
                .FirstOrDefaultAsync(e => e.UserId == userId.Value);

            if (emp == null)
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user != null)
                {
                    emp = await _db.Set<Employee>().AsNoTracking()
                        .Include(e => e.Branch)
                        .FirstOrDefaultAsync(e =>
                            (!string.IsNullOrEmpty(e.EmailAddress) && e.EmailAddress.ToLower() == user.Email.ToLower()) ||
                            (e.FirstName.ToLower() == user.FirstName.ToLower() && e.LastName.ToLower() == user.LastName.ToLower()));
                }
            }

            if (emp == null)
            {
                return NotFound("No employee record linked to current user account.");
            }

            var year = DateTime.Today.Year;
            var balance = await GetOrCreateBalance(emp.Id, year);
            decimal maxLeaves = balance.MaximumPaidLeaveDays;
            decimal usedLeaves = balance.UsedPaidLeaveDays;

            var myLeaves = await _db.PaidLeaveRequests.AsNoTracking()
                .Where(r => r.EmployeeId == emp.Id)
                .OrderByDescending(r => r.StartDate)
                .ToListAsync();

            decimal pendingLeaves = myLeaves
                .Where(r => r.Status == LeaveRequestStatus.Pending && r.StartDate.Year == year)
                .Sum(r => r.DaysRequested);

            decimal remainingLeaves = Math.Max(0, maxLeaves - usedLeaves - pendingLeaves);

            // Attendance history for this employee (last 90 days)
            var since = DateTime.Today.AddDays(-90);
            var attendanceRecords = await _db.AttendanceRecords.AsNoTracking()
                .Where(a => a.EmployeeId == emp.Id && a.AttendanceDate >= since)
                .OrderByDescending(a => a.AttendanceDate)
                .ToListAsync();

            var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            int presentThisMonth = attendanceRecords
                .Count(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= DateTime.Today && a.Status == AttendanceStatus.Present);
            int absentThisMonth = attendanceRecords
                .Count(a => a.AttendanceDate >= monthStart && a.AttendanceDate <= DateTime.Today && a.Status == AttendanceStatus.Absent);

            // Check if present today
            bool isPresentToday = attendanceRecords.Any(a => a.AttendanceDate == DateTime.Today && a.Status == AttendanceStatus.Present);

            // Calculate payroll history for recent months
            var payrollHistory = new List<object>();
            for (int m = 0; m < 3; m++)
            {
                var periodRef = DateTime.Today.AddMonths(-m);
                var pStart = new DateTime(periodRef.Year, periodRef.Month, 1);
                var pEnd = new DateTime(periodRef.Year, periodRef.Month, DateTime.DaysInMonth(periodRef.Year, periodRef.Month));
                if (pEnd > DateTime.Today) pEnd = DateTime.Today;

                var pAtt = attendanceRecords.Where(a => a.AttendanceDate >= pStart && a.AttendanceDate <= pEnd).ToList();
                var pLeaves = myLeaves.Where(l => l.Status == LeaveRequestStatus.Approved && l.EndDate >= pStart && l.StartDate <= pEnd).ToList();
                var payResult = _payroll.Calculate(emp, pStart, pEnd, pAtt, pLeaves);

                payrollHistory.Add(new
                {
                    PeriodName = pStart.ToString("MMMM yyyy"),
                    PeriodStart = pStart.ToString("yyyy-MM-dd"),
                    PeriodEnd = pEnd.ToString("yyyy-MM-dd"),
                    BaseSalary = payResult.PeriodBasePay,
                    GrossPay = payResult.GrossPay,
                    AbsenceDeduction = payResult.AbsenceDeduction,
                    EmployeeTax = payResult.EmployeeTax,
                    NetPay = payResult.NetPay,
                    PresentDays = payResult.PresentDays,
                    PaidLeaveDays = payResult.PaidLeaveDays,
                    UnpaidAbsenceDays = payResult.UnpaidAbsenceDays,
                    PayType = payResult.PayType.ToString(),
                    PayRate = payResult.PayRate
                });
            }

            var response = new
            {
                EmployeeId = emp.Id,
                EmployeeCode = emp.EmployeeCode,
                FullName = $"{emp.LastName}, {emp.FirstName}" + (!string.IsNullOrWhiteSpace(emp.MiddleName) ? $" {emp.MiddleName[0]}." : ""),
                Department = emp.Department,
                Position = emp.Position,
                BranchName = emp.Branch?.Name ?? "Main Branch",
                PayType = emp.PayType.ToString(),
                PayRate = emp.PayRate,
                IsPresentToday = isPresentToday,
                DaysPresentThisMonth = presentThisMonth,
                DaysAbsentThisMonth = absentThisMonth,
                LeaveYear = year,
                MaximumPaidLeaveDays = maxLeaves,
                UsedPaidLeaveDays = usedLeaves,
                PendingLeaveDays = pendingLeaves,
                RemainingLeaveDays = remainingLeaves,
                MonthlyAllowance = Math.Max(0, maxLeaves - usedLeaves),
                AttendanceHistory = attendanceRecords.Select(a => new
                {
                    a.Id,
                    Date = a.AttendanceDate.ToString("yyyy-MM-dd"),
                    DayOfWeek = a.AttendanceDate.DayOfWeek.ToString(),
                    Status = a.Status.ToString(),
                    MarkedAt = a.MarkedAtUtc.ToLocalTime().ToString("hh:mm tt"),
                    Notes = a.Notes ?? ""
                }),
                LeaveRequests = myLeaves.Select(l => new
                {
                    l.Id,
                    StartDate = l.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = l.EndDate.ToString("yyyy-MM-dd"),
                    DaysRequested = l.DaysRequested,
                    Reason = l.Reason,
                    Status = l.Status.ToString(),
                    ApprovedAt = l.ApprovedAtUtc.HasValue ? l.ApprovedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : null
                }),
                SalaryHistory = payrollHistory
            };

            return Ok(response);
        }

        [HttpPost("my-leave-request")]
        [Authorize]
        public async Task<ActionResult> SubmitMyLeaveRequest([FromBody] SubmitLeaveRequestDto request)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return Unauthorized("No user context.");

            var emp = await _db.Set<Employee>().FirstOrDefaultAsync(e => e.UserId == userId.Value);
            if (emp == null)
            {
                var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId.Value);
                if (user != null)
                {
                    emp = await _db.Set<Employee>().FirstOrDefaultAsync(e =>
                        (!string.IsNullOrEmpty(e.EmailAddress) && e.EmailAddress.ToLower() == user.Email.ToLower()) ||
                        (e.FirstName.ToLower() == user.FirstName.ToLower() && e.LastName.ToLower() == user.LastName.ToLower()));
                }
            }

            if (emp == null) return NotFound("No employee profile found for user.");
            if (request.EndDate.Date < request.StartDate.Date)
                return BadRequest("End date cannot be before start date.");

            var days = CountWeekdays(request.StartDate.Date, request.EndDate.Date);
            if (days <= 0) return BadRequest("The selected leave period contains no weekdays.");

            var leave = new PaidLeaveRequest
            {
                EmployeeId = emp.Id,
                StartDate = request.StartDate.Date,
                EndDate = request.EndDate.Date,
                DaysRequested = days,
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Personal Leave" : request.Reason.Trim(),
                RequestedByUserId = userId,
                Status = LeaveRequestStatus.Pending
            };

            _db.PaidLeaveRequests.Add(leave);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                leave.Id,
                StartDate = leave.StartDate.ToString("yyyy-MM-dd"),
                EndDate = leave.EndDate.ToString("yyyy-MM-dd"),
                leave.DaysRequested,
                leave.Reason,
                Status = leave.Status.ToString()
            });
        }

        [HttpGet("leave-balances")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<object>>> GetLeaveBalances(int? year = null)
        {
            var leaveYear = year ?? DateTime.Today.Year;
            var callerBranchId = CallerBranchId();
            var empQuery = _db.Set<Employee>().AsNoTracking()
                .Where(x => x.Status == EmploymentStatus.Active);

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                empQuery = empQuery.Where(x => x.BranchId == callerBranchId.Value);
            }

            var employees = await empQuery.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ToListAsync();
            var empIds = employees.Select(x => x.Id).ToList();

            var balances = await _db.EmployeeLeaveBalances
                .AsNoTracking()
                .Where(x => empIds.Contains(x.EmployeeId) && x.LeaveYear == leaveYear)
                .ToDictionaryAsync(x => x.EmployeeId);

            var result = employees.Select(e =>
            {
                balances.TryGetValue(e.Id, out var bal);
                decimal maxLeaves = bal?.MaximumPaidLeaveDays ?? 12m;
                decimal usedLeaves = bal?.UsedPaidLeaveDays ?? 0m;
                decimal remaining = Math.Max(0, maxLeaves - usedLeaves);

                return new
                {
                    EmployeeId = e.Id,
                    EmployeeCode = e.EmployeeCode,
                    EmployeeName = $"{e.LastName}, {e.FirstName}" + (!string.IsNullOrWhiteSpace(e.MiddleName) ? $" {e.MiddleName[0]}." : ""),
                    Department = e.Department,
                    Position = e.Position,
                    LeaveYear = leaveYear,
                    MaximumPaidLeaveDays = maxLeaves,
                    UsedPaidLeaveDays = usedLeaves,
                    RemainingLeaveDays = remaining
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost("leave-balances")]
        [Authorize(Policy = "RequireManager")]
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
                balance.MaximumPaidLeaveDays = request.MaximumPaidLeaveDays;
            }

            await _db.SaveChangesAsync();
            return Ok(balance);
        }

        [HttpGet("leave-requests")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<IEnumerable<object>>> GetLeaveRequests([FromQuery] LeaveRequestStatus? status = null)
        {
            var callerBranchId = CallerBranchId();
            var query = _db.PaidLeaveRequests.AsNoTracking().Include(x => x.Employee).AsQueryable();

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                query = query.Where(x => x.Employee!.BranchId == callerBranchId.Value);
            }

            if (status is not null) query = query.Where(x => x.Status == status.Value);

            var leaves = await query.OrderByDescending(x => x.StartDate).ToListAsync();
            var empIds = leaves.Select(l => l.EmployeeId).Distinct().ToList();
            var year = DateTime.Today.Year;
            var balances = await _db.EmployeeLeaveBalances.AsNoTracking()
                .Where(b => empIds.Contains(b.EmployeeId) && b.LeaveYear == year)
                .ToDictionaryAsync(b => b.EmployeeId);

            var result = leaves.Select(l =>
            {
                balances.TryGetValue(l.EmployeeId, out var bal);
                decimal maxDays = bal?.MaximumPaidLeaveDays ?? 12m;
                decimal usedDays = bal?.UsedPaidLeaveDays ?? 0m;
                decimal remaining = Math.Max(0, maxDays - usedDays);
                bool wouldExceed = (usedDays + l.DaysRequested) > maxDays;

                return new
                {
                    l.Id,
                    l.EmployeeId,
                    EmployeeCode = l.Employee?.EmployeeCode ?? "",
                    EmployeeName = l.Employee != null ? $"{l.Employee.LastName}, {l.Employee.FirstName}" : "",
                    Department = l.Employee?.Department ?? "",
                    Position = l.Employee?.Position ?? "",
                    StartDate = l.StartDate.ToString("yyyy-MM-dd"),
                    EndDate = l.EndDate.ToString("yyyy-MM-dd"),
                    DaysRequested = l.DaysRequested,
                    Reason = l.Reason,
                    Status = l.Status.ToString(),
                    ApprovedAt = l.ApprovedAtUtc.HasValue ? l.ApprovedAtUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm") : null,
                    QuotaMax = maxDays,
                    QuotaUsed = usedDays,
                    QuotaRemaining = remaining,
                    ExceedsQuota = wouldExceed
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost("leave-requests")]
        [Authorize]
        public async Task<ActionResult<PaidLeaveRequest>> RequestPaidLeave(CreateLeaveRequest request)
        {
            if (request.EndDate.Date < request.StartDate.Date)
                return BadRequest("Leave end date cannot be before the start date.");

            if (!await _db.Set<Employee>().AnyAsync(x => x.Id == request.EmployeeId))
                return NotFound("Employee not found.");

            var days = CountWeekdays(request.StartDate.Date, request.EndDate.Date);
            if (days <= 0) return BadRequest("The selected leave period contains no weekdays.");

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

            // Manager can approve even if it exceeds the fixed amount
            leave.Status = LeaveRequestStatus.Approved;
            leave.ApprovedByUserId = GetCurrentUserId();
            leave.ApprovedAtUtc = DateTime.UtcNow;
            balance.UsedPaidLeaveDays = used + leave.DaysRequested;

            // Auto-mark AttendanceRecords for the approved leave weekdays
            for (var d = leave.StartDate.Date; d <= leave.EndDate.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                {
                    var attRec = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.EmployeeId == leave.EmployeeId && a.AttendanceDate == d);
                    if (attRec != null)
                    {
                        attRec.Status = AttendanceStatus.PaidLeave;
                        attRec.Notes = $"Approved Leave: {leave.Reason}";
                        attRec.MarkedAtUtc = DateTime.UtcNow;
                        attRec.MarkedByUserId = GetCurrentUserId();
                    }
                    else
                    {
                        _db.AttendanceRecords.Add(new AttendanceRecord
                        {
                            EmployeeId = leave.EmployeeId,
                            AttendanceDate = d,
                            Status = AttendanceStatus.PaidLeave,
                            Notes = $"Approved Leave: {leave.Reason}",
                            MarkedAtUtc = DateTime.UtcNow,
                            MarkedByUserId = GetCurrentUserId()
                        });
                    }
                }
            }

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

            var callerBranchId = CallerBranchId();
            var empQuery = _db.Set<Employee>()
                .AsNoTracking()
                .Where(x => x.Status == EmploymentStatus.Active);

            if (!IsAdmin() && callerBranchId.HasValue)
            {
                empQuery = empQuery.Where(x => x.BranchId == callerBranchId.Value);
            }

            var employees = await empQuery
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

            var empBranchDict = employees.ToDictionary(e => e.Id, e => e.BranchId ?? callerBranchId ?? 1);

            foreach (var result in results.Where(x => x.EmployeeTax > 0))
            {
                var sourceReference = $"PayrollTax:{result.EmployeeId}:{start:yyyyMMdd}:{end:yyyyMMdd}";
                if (!await _db.CompanyFinanceTransactions.AnyAsync(x => x.SourceReference == sourceReference))
                {
                    int branchId = empBranchDict.TryGetValue(result.EmployeeId, out var bId) && bId > 0
                        ? bId
                        : (callerBranchId ?? 1);

                    _db.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
                    {
                        Type = FinanceTransactionType.Deduction,
                        Category = "Employee Payroll Tax",
                        Amount = result.EmployeeTax,
                        TransactionDate = end,
                        Description = $"Payroll tax for {result.EmployeeName} ({start:yyyy-MM-dd} to {end:yyyy-MM-dd})",
                        SourceReference = sourceReference,
                        RecordedByUserId = GetCurrentUserId(),
                        BranchId = branchId
                    });
                }
            }

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // In case of any concurrent collision on SourceReference or finance logging, ignore and continue returning calculated payroll
            }

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
            if (end < start) return 0;
            var count = 0;
            for (var date = start; date <= end; date = date.AddDays(1))
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday) count++;
            return count;
        }

        private static DateTime Max(DateTime a, DateTime b) => a > b ? a : b;
        private static DateTime Min(DateTime a, DateTime b) => a < b ? a : b;

        public sealed class BulkAttendanceRequest
        {
            public List<int> EmployeeIds { get; set; } = new();
            public DateTime AttendanceDate { get; set; } = DateTime.Today;
            public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
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

        public sealed class SubmitLeaveRequestDto
        {
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public string Reason { get; set; } = string.Empty;
        }
    }
}
