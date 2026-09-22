using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public InventoryController(MasterErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetActive() =>
            Ok(await _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Where(x => x.Status != InventoryStatus.Disposed)
                .OrderByDescending(x => x.DateReceived)
                .ToListAsync());

        [HttpGet("summary-by-category")]
        public async Task<IActionResult> GetSummaryByCategory()
        {
            var summary = await _db.Set<Inventory>()
                .Where(x => x.Status == InventoryStatus.InStock)
                .Include(x => x.DeviceCategory)
                .GroupBy(x => x.DeviceCategory!.Name)
                .Select(g => new { CategoryName = g.Key, AvailableCount = g.Count() })
                .ToListAsync();

            return Ok(summary);
        }
        [HttpPost]
        [Authorize(Policy = "RequireManager")]
        public async Task<IActionResult> Create(Inventory item)
        {
            if (item.DateReceived == default) item.DateReceived = DateTime.UtcNow;
            _db.Add(item);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetActive), new { id = item.Id }, item);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, Inventory updated)
        {
            var existing = await _db.Set<Inventory>().FindAsync(id);
            if (existing is null) return NotFound();
            existing.DeviceName = updated.DeviceName;
            existing.DeviceCategoryId = updated.DeviceCategoryId;
            existing.SerialNumber = updated.SerialNumber;
            existing.Status = updated.Status;
            existing.Notes = updated.Notes;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Set<Inventory>().FindAsync(id);
            if (existing is null) return NotFound();
            _db.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}