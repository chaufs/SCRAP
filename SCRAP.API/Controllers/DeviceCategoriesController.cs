using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceCategoriesController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public DeviceCategoriesController(MasterErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Set<DeviceCategory>().AsNoTracking().OrderBy(x => x.Name).ToListAsync());

        [HttpPost]
        public async Task<IActionResult> Create(DeviceCategory category)
        {
            _db.Add(category);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetAll), new { id = category.Id }, category);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, DeviceCategory updated)
        {
            var existing = await _db.Set<DeviceCategory>().FindAsync(id);
            if (existing is null) return NotFound();
            existing.Name = updated.Name;
            existing.Description = updated.Description;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _db.Set<DeviceCategory>().FindAsync(id);
            if (existing is null) return NotFound();
            _db.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("{categoryId:int}/yields")]
        public async Task<IActionResult> GetYields(int categoryId) =>
            Ok(await _db.Set<ArchetypeRecipe>().AsNoTracking()
                .Where(x => x.DeviceCategoryId == categoryId).ToListAsync());

        [HttpPost("{categoryId:int}/yields")]
        public async Task<IActionResult> AddYield(int categoryId, ArchetypeRecipe recipe)
        {
            recipe.DeviceCategoryId = categoryId;
            _db.Add(recipe);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(GetYields), new { categoryId }, recipe);
        }

        [HttpDelete("yields/{id:int}")]
        public async Task<IActionResult> DeleteYield(int id)
        {
            var existing = await _db.Set<ArchetypeRecipe>().FindAsync(id);
            if (existing is null) return NotFound();
            _db.Remove(existing);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}