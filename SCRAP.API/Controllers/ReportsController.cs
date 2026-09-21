using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.infrastructure.data;
using SCRAP.domain.entities;
using SCRAP.API.Services;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly MasterErpDbContext _db;
        public ReportsController(MasterErpDbContext db)
        {
            _db = db;
        }

        [HttpGet("inventory/status-summary")]
        public async Task<ActionResult<IEnumerable<object>>> InventoryStatusSummary()
        {
            var data = await _db.Inventories
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("sales/total")]
        public async Task<ActionResult<object>> SalesTotal([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var q = _db.Sales.AsQueryable();
            if (from.HasValue) q = q.Where(s => s.SaleDate >= from.Value);
            if (to.HasValue) q = q.Where(s => s.SaleDate <= to.Value);
            var total = await q.SumAsync(s => (decimal?)s.Amount) ?? 0m;
            return Ok(new { Total = total });
        }

        // ===== Inventory history =====

        [HttpGet("inventory-history")]
        public async Task<IActionResult> GetInventoryHistory() =>
            Ok(await _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .OrderByDescending(x => x.DateReceived)
                .ToListAsync());

        [HttpGet("inventory-history/pdf")]
        public async Task<IActionResult> GetInventoryHistoryPdf()
        {
            var items = await _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .OrderByDescending(x => x.DateReceived)
                .ToListAsync();

            var pdf = ReportPdfGenerator.GenerateInventoryReport(items);
            return File(pdf, "application/pdf", $"InventoryHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ===== Teardown history =====

        [HttpGet("teardown-history")]
        public async Task<IActionResult> GetTeardownHistory() =>
            Ok(await _db.TeardownBatches.AsNoTracking()
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .OrderByDescending(b => b.DateProcessed)
                .ToListAsync());

        [HttpGet("teardown-history/pdf")]
        public async Task<IActionResult> GetTeardownHistoryPdf()
        {
            var batches = await _db.TeardownBatches.AsNoTracking()
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .OrderByDescending(b => b.DateProcessed)
                .ToListAsync();

            var pdf = ReportPdfGenerator.GenerateTeardownReport(batches);
            return File(pdf, "application/pdf", $"TeardownHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ===== Commodity sales history (CommoditySale, not the legacy Sales entity above) =====

        [HttpGet("commodity-sales-history")]
        public async Task<IActionResult> GetCommoditySalesHistory() =>
            Ok(await _db.Set<CommoditySale>().AsNoTracking()
                .OrderByDescending(x => x.SaleDate)
                .ToListAsync());

        [HttpGet("commodity-sales-history/pdf")]
        public async Task<IActionResult> GetCommoditySalesHistoryPdf()
        {
            var sales = await _db.Set<CommoditySale>().AsNoTracking()
                .OrderByDescending(x => x.SaleDate)
                .ToListAsync();

            var pdf = ReportPdfGenerator.GenerateSalesReport(sales);
            return File(pdf, "application/pdf", $"SalesHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }
        [HttpGet("overall/pdf")]
        public async Task<IActionResult> GetOverallReportPdf()
        {
            var inventory = await _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .OrderByDescending(x => x.DateReceived)
                .ToListAsync();

            var teardowns = await _db.TeardownBatches.AsNoTracking()
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .OrderByDescending(b => b.DateProcessed)
                .ToListAsync();

            var sales = await _db.Set<CommoditySale>().AsNoTracking()
                .OrderByDescending(x => x.SaleDate)
                .ToListAsync();

            var pdf = ReportPdfGenerator.GenerateOverallReport(inventory, teardowns, sales);
            return File(pdf, "application/pdf", $"OverallReport_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}