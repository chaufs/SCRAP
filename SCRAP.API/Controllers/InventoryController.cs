using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using System.Security.Claims;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public InventoryController(TenantErpDbContext db) => _db = db;

        /// <summary>Returns the caller's BranchId, or null if they are Admin (sees all).</summary>
        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        private bool IsAdmin() =>
            User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet]
        public async Task<IActionResult> GetActive([FromQuery] int? branchId = null)
        {
            IQueryable<Inventory> q = _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Include(x => x.Branch)
                .Where(x => x.Status != InventoryStatus.Disposed);

            if (!IsAdmin())
            {
                var userBranchId = CallerBranchId() ?? -1;
                q = q.Where(x => x.BranchId == userBranchId);
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                q = q.Where(x => x.BranchId == branchId.Value);
            }

            return Ok(await q.OrderByDescending(x => x.DateReceived).ToListAsync());
        }

        [HttpGet("summary-by-category")]
        public async Task<IActionResult> GetSummaryByCategory([FromQuery] int? branchId = null)
        {
            IQueryable<Inventory> q = _db.Set<Inventory>().Where(x => x.Status == InventoryStatus.InStock)
                .Include(x => x.DeviceCategory);

            if (!IsAdmin())
            {
                var userBranchId = CallerBranchId() ?? -1;
                q = q.Where(x => x.BranchId == userBranchId);
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                q = q.Where(x => x.BranchId == branchId.Value);
            }

            var summary = await q
                .GroupBy(x => x.DeviceCategory!.Name)
                .Select(g => new { CategoryName = g.Key, AvailableCount = g.Count() })
                .ToListAsync();

            return Ok(summary);
        }

        [HttpGet("stock-levels")]
        public async Task<IActionResult> GetStockLevels([FromQuery] int? branchId = null)
        {
            var targetBranch = !IsAdmin() ? CallerBranchId() : branchId;

            var categories = await _db.DeviceCategories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();

            var invQ = _db.Set<Inventory>().AsNoTracking().AsQueryable();
            if (targetBranch.HasValue && targetBranch.Value > 0)
            {
                invQ = invQ.Where(i => i.BranchId == targetBranch.Value);
            }

            var inStockCounts = await invQ
                .Where(i => i.Status == InventoryStatus.InStock)
                .GroupBy(i => i.DeviceCategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

            var inTeardownCounts = await invQ
                .Where(i => i.Status == InventoryStatus.InTeardown)
                .GroupBy(i => i.DeviceCategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

            var totalProcessedCounts = await invQ
                .Where(i => i.Status == InventoryStatus.Disposed)
                .GroupBy(i => i.DeviceCategoryId)
                .Select(g => new { CategoryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.CategoryId, g => g.Count);

            var result = categories.Select(c =>
            {
                int inStock = inStockCounts.TryGetValue(c.Id, out var count) ? count : 0;
                int inTeardown = inTeardownCounts.TryGetValue(c.Id, out var td) ? td : 0;
                int disposed = totalProcessedCounts.TryGetValue(c.Id, out var dp) ? dp : 0;

                string alertLevel;
                if (inStock == 0) alertLevel = "OutOfStock";
                else if (inStock <= 5) alertLevel = "LowStock";
                else alertLevel = "Normal";

                return new
                {
                    CategoryId = c.Id,
                    CategoryName = c.Name,
                    Description = c.Description ?? "",
                    InStockCount = inStock,
                    InTeardownCount = inTeardown,
                    DisposedCount = disposed,
                    TotalReceived = inStock + inTeardown + disposed,
                    AlertLevel = alertLevel,
                    IsLowStock = inStock <= 5
                };
            }).ToList();

            return Ok(result);
        }

        [HttpPost]
        [Authorize(Policy = "RequireManager")]
        public async Task<IActionResult> Create(Inventory item)
        {
            if (item.DateReceived == default) item.DateReceived = DateTime.UtcNow;

            // Non-admin must stamp their branch
            if (!IsAdmin())
            {
                var branchId = CallerBranchId() ?? 0;
                if (branchId <= 0) return BadRequest("User has no branch assigned.");
                item.BranchId = branchId;
            }
            else if (item.BranchId <= 0)
            {
                return BadRequest("BranchId is required.");
            }

            _db.Add(item);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetActive), new { id = item.Id }, item);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Inventory updated)
        {
            var existing = await _db.Set<Inventory>().FindAsync(id);
            if (existing is null) return NotFound();

            // Non-admin can only edit their own branch's items
            if (!IsAdmin())
            {
                var branchId = CallerBranchId() ?? 0;
                if (branchId <= 0 || existing.BranchId != branchId)
                    return Forbid();
            }

            existing.DeviceName       = updated.DeviceName;
            existing.DeviceCategoryId = updated.DeviceCategoryId;
            existing.SerialNumber     = updated.SerialNumber;
            existing.Status           = updated.Status;
            existing.Notes            = updated.Notes;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Set<Inventory>().FindAsync(id);
            if (existing is null) return NotFound();

            if (!IsAdmin())
            {
                var branchId = CallerBranchId() ?? 0;
                if (branchId <= 0 || existing.BranchId != branchId)
                    return Forbid();
            }

            _db.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}