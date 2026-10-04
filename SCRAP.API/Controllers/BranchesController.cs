using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using System.Security.Claims;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class BranchesController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public BranchesController(TenantErpDbContext db) => _db = db;

        // ── CRUD ─────────────────────────────────────────────────────────────

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Branches.AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync());

        [HttpGet("{id:int}")]
        public async Task<IActionResult> Get(int id)
        {
            var branch = await _db.Branches.AsNoTracking().FirstOrDefaultAsync(b => b.Id == id);
            return branch is null ? NotFound() : Ok(branch);
        }

        [HttpGet("next-code")]
        public async Task<IActionResult> GetNextCode()
        {
            var code = await GenerateNextBranchCode();
            return Ok(new { code });
        }

        private async Task<string> GenerateNextBranchCode()
        {
            var codes = await _db.Branches.Select(b => b.Code).ToListAsync();
            int max = 0;
            foreach (var code in codes)
            {
                var digits = new string(code.Where(char.IsDigit).ToArray());
                if (int.TryParse(digits, out var n) && n > max) max = n;
            }
            return $"BR-{(max + 1):D3}";
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Branch name is required.");

            // Auto-increment branch code
            string code = string.IsNullOrWhiteSpace(req.Code)
                ? await GenerateNextBranchCode()
                : req.Code.Trim().ToUpper();

            while (await _db.Branches.AnyAsync(b => b.Code == code))
            {
                var codes = await _db.Branches.Select(b => b.Code).ToListAsync();
                int max = 0;
                foreach (var c in codes)
                {
                    var digits = new string(c.Where(char.IsDigit).ToArray());
                    if (int.TryParse(digits, out var n) && n > max) max = n;
                }
                code = $"BR-{(max + 1):D3}";
            }

            var branch = new Branch
            {
                Name    = req.Name.Trim(),
                Code    = code,
                Address = req.Address?.Trim(),
                IsActive = true
            };
            _db.Branches.Add(branch);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = branch.Id }, branch);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] BranchRequest req)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch is null) return NotFound();

            if (!string.IsNullOrWhiteSpace(req.Name))    branch.Name    = req.Name.Trim();
            if (!string.IsNullOrWhiteSpace(req.Code))    branch.Code    = req.Code.Trim().ToUpper();
            if (req.Address is not null)                  branch.Address = req.Address.Trim();
            if (req.IsActive.HasValue)                   branch.IsActive = req.IsActive.Value;

            await _db.SaveChangesAsync();
            return Ok(branch);
        }

        // ── User assignment ──────────────────────────────────────────────────

        [HttpPost("{id:int}/assign-user/{userId:int}")]
        public async Task<IActionResult> AssignUser(int id, int userId)
        {
            var branch = await _db.Branches.FindAsync(id);
            if (branch is null) return NotFound("Branch not found.");

            var user = await _db.Users.FindAsync(userId);
            if (user is null) return NotFound("User not found.");

            user.BranchId = id;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"{user.Username} assigned to branch '{branch.Name}'." });
        }

        [HttpPost("{id:int}/unassign-user/{userId:int}")]
        public async Task<IActionResult> UnassignUser(int id, int userId)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user is null) return NotFound("User not found.");

            user.BranchId = null;
            await _db.SaveChangesAsync();
            return Ok(new { message = $"{user.Username} unassigned from branch." });
        }

        [HttpGet("{id:int}/users")]
        public async Task<IActionResult> GetUsers(int id) =>
            Ok(await _db.Users.AsNoTracking()
                .Where(u => u.BranchId == id)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName, Role = u.Role.ToString() })
                .ToListAsync());

        [HttpGet("unassigned-users")]
        public async Task<IActionResult> GetUnassignedUsers() =>
            Ok(await _db.Users.AsNoTracking()
                .Where(u => u.BranchId == null && u.Role != UserRole.Admin)
                .Select(u => new { u.Id, u.Username, u.FirstName, u.LastName, Role = u.Role.ToString() })
                .ToListAsync());

        // ── Overview (all branches with key stats) ────────────────────────────

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var branches = await _db.Branches.AsNoTracking()
                .OrderBy(b => b.Name)
                .ToListAsync();

            var inventoryCounts = await _db.Inventories.AsNoTracking()
                .Where(i => i.Status != InventoryStatus.Disposed)
                .GroupBy(i => i.BranchId)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToListAsync();

            var salesTotals = await _db.CommoditySales.AsNoTracking()
                .GroupBy(s => s.BranchId)
                .Select(g => new { BranchId = g.Key, Total = g.Sum(s => s.TotalAmount) })
                .ToListAsync();

            var teardownCounts = await _db.TeardownBatches.AsNoTracking()
                .GroupBy(t => t.BranchId)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToListAsync();

            var userCounts = await _db.Users.AsNoTracking()
                .Where(u => u.BranchId.HasValue)
                .GroupBy(u => u.BranchId!.Value)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .ToListAsync();

            var overview = branches.Select(b => new
            {
                b.Id,
                b.Name,
                b.Code,
                b.Address,
                b.IsActive,
                ActiveInventory = inventoryCounts.FirstOrDefault(x => x.BranchId == b.Id)?.Count ?? 0,
                TotalRevenue    = salesTotals.FirstOrDefault(x => x.BranchId == b.Id)?.Total ?? 0m,
                TeardownBatches = teardownCounts.FirstOrDefault(x => x.BranchId == b.Id)?.Count ?? 0,
                StaffCount      = userCounts.FirstOrDefault(x => x.BranchId == b.Id)?.Count ?? 0
            });

            return Ok(overview);
        }

        [HttpGet("revenue-comparison")]
        public async Task<IActionResult> GetRevenueComparison([FromQuery] int months = 6)
        {
            if (months < 2) months = 6;
            if (months > 24) months = 24;

            var branches = await _db.Branches.AsNoTracking()
                .Where(b => b.IsActive)
                .OrderBy(b => b.Id)
                .ToListAsync();

            var now = DateTime.Today;
            var monthDates = new List<DateTime>();
            for (int i = months - 1; i >= 0; i--)
            {
                var d = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                monthDates.Add(d);
            }

            var xLabels = monthDates.Select(d => d.ToString("MMM yyyy")).ToList();
            var startDate = monthDates[0];
            var endDateExclusive = monthDates[^1].AddMonths(1);

            var sales = await _db.CommoditySales.AsNoTracking()
                .Where(s => s.SaleDate >= startDate && s.SaleDate < endDateExclusive)
                .Select(s => new { s.BranchId, s.TotalAmount, s.SaleDate })
                .ToListAsync();

            var txns = await _db.CompanyFinanceTransactions.AsNoTracking()
                .Where(t => t.Type == FinanceTransactionType.Income && t.TransactionDate >= startDate && t.TransactionDate < endDateExclusive)
                .Select(t => new { t.BranchId, t.Amount, t.TransactionDate, t.Category })
                .ToListAsync();

            var colors = new[]
            {
                "#15803D", // Green (like ZitCoin in user image)
                "#7E22CE", // Purple (like Kelsa in user image)
                "#0069E5", // Blue
                "#EA580C", // Orange
                "#0D9488", // Teal
                "#DC2626"  // Red
            };

            var seriesList = new List<object>();
            int colorIdx = 0;

            foreach (var b in branches)
            {
                var values = new List<decimal>();
                foreach (var m in monthDates)
                {
                    var nextM = m.AddMonths(1);
                    var csTotal = sales.Where(s => s.BranchId == b.Id && s.SaleDate >= m && s.SaleDate < nextM).Sum(s => s.TotalAmount);
                    var txnTotal = txns.Where(t => t.BranchId == b.Id && t.TransactionDate >= m && t.TransactionDate < nextM &&
                        (t.Category.Contains("Sale", StringComparison.OrdinalIgnoreCase) || t.Category.Contains("Revenue", StringComparison.OrdinalIgnoreCase)))
                        .Sum(t => t.Amount);

                    values.Add(Math.Max(csTotal, txnTotal));
                }

                seriesList.Add(new
                {
                    BranchId = b.Id,
                    Name = $"{b.Name} ({b.Code})",
                    Color = colors[colorIdx % colors.Length],
                    Values = values
                });

                colorIdx++;
            }

            return Ok(new
            {
                XLabels = xLabels,
                Series = seriesList
            });
        }

        // ── Request DTOs ─────────────────────────────────────────────────────

        public class BranchRequest
        {
            public string Name     { get; set; } = string.Empty;
            public string Code     { get; set; } = string.Empty;
            public string? Address { get; set; }
            public bool? IsActive  { get; set; }
        }
    }
}
