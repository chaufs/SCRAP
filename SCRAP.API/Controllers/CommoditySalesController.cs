using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using System.Security.Claims;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CommoditySalesController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public CommoditySalesController(TenantErpDbContext db) => _db = db;

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) ? id : (int?)null;
        }
        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? branchId = null)
        {
            var q = _db.Set<CommoditySale>().AsNoTracking()
                .Include(x => x.Branch)
                .OrderByDescending(x => x.SaleDate);

            if (!IsAdmin())
            {
                var userBranchId = CallerBranchId() ?? -1;
                return Ok(await q.Where(x => x.BranchId == userBranchId).ToListAsync());
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                return Ok(await q.Where(x => x.BranchId == branchId.Value).ToListAsync());
            }

            return Ok(await q.ToListAsync());
        }

        [HttpPost]
        public async Task<IActionResult> LogSale([FromBody] CommoditySale sale)
        {
            if (string.IsNullOrWhiteSpace(sale.MaterialName)) return BadRequest("Material name is required.");
            if (string.IsNullOrWhiteSpace(sale.BuyerName))   return BadRequest("Buyer name is required.");
            if (string.IsNullOrWhiteSpace(sale.InvoiceNumber)) return BadRequest("Invoice number is required.");
            if (sale.QuantityKg <= 0) return BadRequest("Quantity must be greater than zero.");
            if (sale.PricePerKg <= 0) return BadRequest("Price per kilo must be greater than zero.");

            // Stamp branch
            if (!IsAdmin())
            {
                var branchId = CallerBranchId() ?? 0;
                if (branchId <= 0) return BadRequest("User has no branch assigned.");
                sale.BranchId = branchId;
            }
            else if (sale.BranchId <= 0)
            {
                return BadRequest("BranchId is required.");
            }

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
                _db.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
                {
                    Type            = FinanceTransactionType.Income,
                    Category        = "Sales Revenue",
                    Amount          = sale.TotalAmount,
                    TransactionDate = sale.SaleDate.Date,
                    Description     = $"Commodity sale {sale.InvoiceNumber}",
                    SourceReference = $"CommoditySaleInvoice:{sale.InvoiceNumber}",
                    BranchId        = sale.BranchId
                });
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