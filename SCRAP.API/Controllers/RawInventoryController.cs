using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RawInventoryController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public RawInventoryController(TenantErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Set<RawInventory>().AsNoTracking()
                .OrderByDescending(x => x.CurrentTotalWeightKg)
                .ToListAsync());
    }
}