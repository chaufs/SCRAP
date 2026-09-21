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
        private readonly MasterErpDbContext _db;
        public DashboardController(MasterErpDbContext db) => _db = db;

        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var activeCount = await _db.Set<Inventory>().CountAsync(x => x.Status != InventoryStatus.Disposed);
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