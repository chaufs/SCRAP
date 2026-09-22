using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/finance")]
    public class CompanyFinanceController : ControllerBase
    {
        private readonly MasterErpDbContext _db;

        public CompanyFinanceController(MasterErpDbContext db) => _db = db;

        [HttpGet("dashboard")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<FinanceDashboard>> GetDashboard(DateTime? periodStart = null, DateTime? periodEnd = null)
        {
            var start = (periodStart ?? DateTime.Today.AddMonths(-1)).Date;
            var end = (periodEnd ?? DateTime.Today).Date;
            if (end < start) return BadRequest("Finance period end cannot be before the start date.");

            var transactions = await _db.CompanyFinanceTransactions
                .AsNoTracking()
                .Where(x => x.TransactionDate.Date >= start && x.TransactionDate.Date <= end)
                .OrderByDescending(x => x.TransactionDate)
                .ToListAsync();

            var income = transactions.Where(x => x.Type == FinanceTransactionType.Income).Sum(x => x.Amount);
            var deductions = transactions.Where(x => x.Type == FinanceTransactionType.Deduction).Sum(x => x.Amount);
            var payrollTaxes = transactions
                .Where(x => x.Type == FinanceTransactionType.Deduction && x.Category == "Employee Payroll Tax")
                .Sum(x => x.Amount);

            return Ok(new FinanceDashboard
            {
                PeriodStart = start,
                PeriodEnd = end,
                TotalIncome = income,
                TotalDeductions = deductions,
                PayrollTaxes = payrollTaxes,
                CompanyBalance = income - deductions,
                Transactions = transactions
            });
        }

        [HttpPost("deductions")]
        [Authorize(Policy = "RequireManager")]
        public async Task<ActionResult<CompanyFinanceTransaction>> AddDeduction(AddDeductionRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Category)) return BadRequest("Deduction category is required.");
            if (request.Amount <= 0) return BadRequest("Deduction amount must be greater than zero.");

            var transaction = new CompanyFinanceTransaction
            {
                Type = FinanceTransactionType.Deduction,
                Category = request.Category.Trim(),
                Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                TransactionDate = (request.TransactionDate ?? DateTime.UtcNow).Date,
                Description = string.IsNullOrWhiteSpace(request.Description) ? request.Category.Trim() : request.Description.Trim(),
                RecordedByUserId = GetCurrentUserId()
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

            var transaction = new CompanyFinanceTransaction
            {
                Type = FinanceTransactionType.Income,
                Category = request.Category.Trim(),
                Amount = Math.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
                TransactionDate = (request.TransactionDate ?? DateTime.UtcNow).Date,
                Description = string.IsNullOrWhiteSpace(request.Description) ? request.Category.Trim() : request.Description.Trim(),
                RecordedByUserId = GetCurrentUserId()
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
        }

        public sealed class AddIncomeRequest
        {
            public string Category { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public DateTime? TransactionDate { get; set; }
            public string? Description { get; set; }
        }

        public sealed class FinanceDashboard
        {
            public DateTime PeriodStart { get; set; }
            public DateTime PeriodEnd { get; set; }
            public decimal TotalIncome { get; set; }
            public decimal TotalDeductions { get; set; }
            public decimal PayrollTaxes { get; set; }
            public decimal CompanyBalance { get; set; }
            public IReadOnlyCollection<CompanyFinanceTransaction> Transactions { get; set; } = Array.Empty<CompanyFinanceTransaction>();
        }
    }
}
