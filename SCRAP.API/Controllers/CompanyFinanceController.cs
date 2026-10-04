using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using System.Security.Claims;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/finance")]
    public class CompanyFinanceController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public CompanyFinanceController(TenantErpDbContext db) => _db = db;

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            if (int.TryParse(val, out var id) && id > 0) return id;

            var userIdVal = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdVal, out var uid))
            {
                var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Id == uid);
                if (user?.BranchId.HasValue == true && user.BranchId.Value > 0)
                    return user.BranchId.Value;

                var emp = _db.Set<Employee>().AsNoTracking().FirstOrDefault(e => e.UserId == uid);
                if (emp?.BranchId.HasValue == true && emp.BranchId.Value > 0)
                    return emp.BranchId.Value;
            }

            var username = User.FindFirstValue(ClaimTypes.Name);
            if (!string.IsNullOrEmpty(username))
            {
                var user = _db.Users.AsNoTracking().FirstOrDefault(u => u.Username == username);
                if (user?.BranchId.HasValue == true && user.BranchId.Value > 0)
                    return user.BranchId.Value;
            }

            return null;
        }

        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet("dashboard")]
        [Authorize]
        public async Task<ActionResult<FinanceDashboard>> GetDashboard(
            [FromQuery] DateTime? periodStart = null,
            [FromQuery] DateTime? periodEnd = null,
            [FromQuery] int? branchId = null)
        {
            var start = (periodStart ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
            var end   = (periodEnd   ?? DateTime.Today).Date;
            if (end < start) return BadRequest("Finance period end cannot be before the start date.");
            var endExclusive = end.AddDays(1);

            var q = _db.CompanyFinanceTransactions.AsNoTracking()
                .Include(x => x.Branch)
                .Where(x => x.TransactionDate >= start && x.TransactionDate < endExclusive);

            if (!IsAdmin())
            {
                // Non-admin (Manager, etc.) can ONLY see their assigned branch
                var userBranchId = CallerBranchId() ?? branchId ?? -1;
                q = q.Where(x => x.BranchId == userBranchId);
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                // Admin can filter by branch
                q = q.Where(x => x.BranchId == branchId.Value);
            }

            var transactions = await q.OrderByDescending(x => x.TransactionDate).ToListAsync();

            var income       = transactions.Where(x => x.Type == FinanceTransactionType.Income).Sum(x => x.Amount);
            var deductions   = transactions.Where(x => x.Type == FinanceTransactionType.Deduction).Sum(x => x.Amount);
            var payrollTaxes = transactions
                .Where(x => x.Type == FinanceTransactionType.Deduction && x.Category == "Employee Payroll Tax")
                .Sum(x => x.Amount);

            var isInvestOrDonate = (CompanyFinanceTransaction t) =>
                t.Type == FinanceTransactionType.Income && (
                    t.Category.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                    t.Category.Contains("Donat", StringComparison.OrdinalIgnoreCase) ||
                    t.Category.Contains("Grant", StringComparison.OrdinalIgnoreCase) ||
                    t.Category.Contains("Capital", StringComparison.OrdinalIgnoreCase) ||
                    t.Description.Contains("Invest", StringComparison.OrdinalIgnoreCase) ||
                    t.Description.Contains("Donat", StringComparison.OrdinalIgnoreCase)
                );

            var investedOrDonated = transactions.Where(isInvestOrDonate).Sum(x => x.Amount);
            var operatingRevenue  = income - investedOrDonated;

            return Ok(new FinanceDashboard
            {
                PeriodStart              = start,
                PeriodEnd                = end,
                TotalIncome              = income,
                TotalDeductions          = deductions,
                PayrollTaxes             = payrollTaxes,
                CompanyBalance           = income - deductions,
                TotalInvestedOrDonated   = investedOrDonated,
                OperatingRevenue         = operatingRevenue,
                Transactions             = transactions
            });
        }

        [HttpPost("deductions")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<CompanyFinanceTransaction>> AddDeduction(AddDeductionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Category)) return BadRequest("Deduction category is required.");
            if (request.Amount <= 0) return BadRequest("Deduction amount must be greater than zero.");

            var branchId = !IsAdmin() ? (CallerBranchId() ?? request.BranchId ?? 0) : (request.BranchId ?? CallerBranchId() ?? 0);
            if (branchId <= 0) return BadRequest("User has no branch assigned.");

            var transaction = new CompanyFinanceTransaction
            {
                Type             = FinanceTransactionType.Deduction,
                Category         = request.Category.Trim(),
                Amount           = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                TransactionDate  = (request.TransactionDate ?? DateTime.UtcNow).Date,
                Description      = string.IsNullOrWhiteSpace(request.Description) ? request.Category.Trim() : request.Description.Trim(),
                RecordedByUserId = GetCurrentUserId(),
                BranchId         = branchId
            };
            _db.CompanyFinanceTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            return Ok(transaction);
        }

        [HttpPost("income")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<CompanyFinanceTransaction>> AddIncome(AddIncomeRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Category)) return BadRequest("Income category is required.");
            if (request.Amount <= 0) return BadRequest("Income amount must be greater than zero.");

            var branchId = !IsAdmin() ? (CallerBranchId() ?? request.BranchId ?? 0) : (request.BranchId ?? CallerBranchId() ?? 0);
            if (branchId <= 0) return BadRequest("User has no branch assigned.");

            var transaction = new CompanyFinanceTransaction
            {
                Type             = FinanceTransactionType.Income,
                Category         = request.Category.Trim(),
                Amount           = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                TransactionDate  = (request.TransactionDate ?? DateTime.UtcNow).Date,
                Description      = string.IsNullOrWhiteSpace(request.Description) ? request.Category.Trim() : request.Description.Trim(),
                RecordedByUserId = GetCurrentUserId(),
                BranchId         = branchId
            };
            _db.CompanyFinanceTransactions.Add(transaction);
            await _db.SaveChangesAsync();
            return Ok(transaction);
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        public sealed class AddDeductionRequest
        {
            public string Category { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime? TransactionDate { get; set; }
            public string? Description { get; set; }
            public int? BranchId { get; set; }
        }

        public sealed class AddIncomeRequest
        {
            public string Category { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime? TransactionDate { get; set; }
            public string? Description { get; set; }
            public int? BranchId { get; set; }
        }

        public sealed class FinanceDashboard
        {
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal PayrollTaxes { get; set; }
            public decimal CompanyBalance { get; set; }
            public decimal TotalInvestedOrDonated { get; set; }
            public decimal OperatingRevenue { get; set; }
            public IReadOnlyCollection<CompanyFinanceTransaction> Transactions { get; set; } = Array.Empty<CompanyFinanceTransaction>();
        }
    }
}
