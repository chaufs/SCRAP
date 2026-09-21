using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.infrastructure.data;
using SCRAP.domain.entities;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ArchetypeRecipesController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public ArchetypeRecipesController(MasterErpDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ArchetypeRecipe>>> GetAll() => await _db.ArchetypeRecipes.ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<ArchetypeRecipe>> Get(int id)
        {
            var item = await _db.ArchetypeRecipes.FindAsync(id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpPost]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<ActionResult<ArchetypeRecipe>> Create(ArchetypeRecipe model)
        {
            _db.ArchetypeRecipes.Add(model);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = model.Id }, model);
        }

        [HttpPut("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Update(int id, ArchetypeRecipe model)
        {
            if (id != model.Id) return BadRequest();
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.ArchetypeRecipes.FindAsync(id);
            if (item == null) return NotFound();
            _db.ArchetypeRecipes.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
