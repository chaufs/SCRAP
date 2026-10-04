using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StorageDestructionController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public StorageDestructionController(TenantErpDbContext db) => _db = db;

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending() =>
            Ok(await _db.Set<StorageDestructionRecord>()
                .Include(x => x.Inventory).ThenInclude(i => i!.DeviceCategory)
                .Where(x => x.Status == StorageDestructionStatus.PendingDestruction)
                .OrderBy(x => x.CreatedAt)
                .ToListAsync());
    }
}