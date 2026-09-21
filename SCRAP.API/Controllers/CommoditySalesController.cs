using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommoditySalesController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public CommoditySalesController(MasterErpDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> GetAll() =>
            Ok(await _db.Set<CommoditySale>().AsNoTracking()
                .OrderByDescending(x => x.SaleDate)
                .ToListAsync());

        [HttpPost]
        public async Task<IActionResult> LogSale([FromBody] CommoditySale sale)
        {
            if (string.IsNullOrWhiteSpace(sale.MaterialName)) return BadRequest("Material name is required.");
            if (string.IsNullOrWhiteSpace(sale.BuyerName)) return BadRequest("Buyer name is required.");
            if (string.IsNullOrWhiteSpace(sale.InvoiceNumber)) return BadRequest("Invoice number is required.");
            if (sale.QuantityKg <= 0) return BadRequest("Quantity must be greater than zero.");
            if (sale.PricePerKg <= 0) return BadRequest("Price per kilo must be greater than zero.");

            var raw = await _db.RawInventories
                .FirstOrDefaultAsync(r => r.MaterialName.ToLower() == sale.MaterialName.ToLower());

            if (raw == null || raw.CurrentTotalWeightKg < sale.QuantityKg)
                return BadRequest($"Not enough {sale.MaterialName} available. Current stock: {raw?.CurrentTotalWeightKg ?? 0} kg.");

            sale.TotalAmount = sale.QuantityKg * sale.PricePerKg;
            if (sale.SaleDate == default) sale.SaleDate = DateTime.UtcNow;

            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                raw.CurrentTotalWeightKg -= sale.QuantityKg;
                _db.Add(sale);
                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return CreatedAtAction(nameof(GetAll), new { id = sale.Id }, sale);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new { message = "Database update error", detail = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}