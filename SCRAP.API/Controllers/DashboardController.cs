using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public DashboardController(TenantErpDbContext db) => _db = db;

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet("summary")]
        public async Task<IActionResult> Summary([FromQuery] int? branchId = null)
        {
            var activeQuery = _db.Set<Inventory>().Where(x => x.Status != InventoryStatus.Disposed);

            if (!IsAdmin())
            {
                var userBranchId = CallerBranchId() ?? -1;
                activeQuery = activeQuery.Where(x => x.BranchId == userBranchId);
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                activeQuery = activeQuery.Where(x => x.BranchId == branchId.Value);
            }

            var activeCount = await activeQuery.CountAsync();
            var categoryCount = await _db.Set<DeviceCategory>().CountAsync();
            var totalRecoveredKg = await _db.Set<RawInventory>().SumAsync(x => (decimal?)x.CurrentTotalWeightKg) ?? 0;

            return Ok(new
            {
                activeInventoryCount = activeCount,
                deviceCategoryCount = categoryCount,
                totalRecoveredWeightKg = totalRecoveredKg
            });
        }
    }
}