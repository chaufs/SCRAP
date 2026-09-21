using Microsoft.AspNetCore.Mvc;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DebugController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public DebugController(MasterErpDbContext db)
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
