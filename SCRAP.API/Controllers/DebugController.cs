using Microsoft.AspNetCore.Mvc;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public DebugController(TenantErpDbContext db)
        {
            _db = db;
        }

        [HttpGet("categories-count")]
        public ActionResult<object> CategoriesCount()
        {
            var count = _db.DeviceCategories.Count();
            return Ok(new { Count = count });
        }
    }
}
