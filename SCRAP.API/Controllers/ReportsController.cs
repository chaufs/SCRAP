using System.Security.Claims;
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
        private readonly TenantErpDbContext _db;
        public ReportsController(TenantErpDbContext db)
        {
            _db = db;
        }

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) ? id : (int?)null;
        }

        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        private int? EffectiveBranchId(int? queryBranchId)
        {
            if (!IsAdmin()) return CallerBranchId() ?? -1;
            return (queryBranchId.HasValue && queryBranchId.Value > 0) ? queryBranchId.Value : (int?)null;
        }

        [HttpGet("inventory/status-summary")]
        public async Task<ActionResult<IEnumerable<object>>> InventoryStatusSummary([FromQuery] int? branchId = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.Inventories.AsQueryable();
            if (targetBranch.HasValue) q = q.Where(i => i.BranchId == targetBranch.Value);

            var data = await q
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();
            return Ok(data);
        }

        [HttpGet("sales/total")]
        public async Task<ActionResult<object>> SalesTotal([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? branchId = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.CommoditySales.AsQueryable();
            if (targetBranch.HasValue) q = q.Where(s => s.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(s => s.SaleDate >= from.Value);
            if (to.HasValue) q = q.Where(s => s.SaleDate <= to.Value);
            var total = await q.SumAsync(s => (decimal?)s.TotalAmount) ?? 0m;
            return Ok(new { Total = total });
        }

        // ===== Inventory history =====

        [HttpGet("inventory-history")]
        public async Task<IActionResult> GetInventoryHistory([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.DateReceived >= from.Value);
            if (to.HasValue) q = q.Where(x => x.DateReceived <= to.Value);

            return Ok(await q.OrderByDescending(x => x.DateReceived).ToListAsync());
        }

        [HttpGet("inventory-history/pdf")]
        public async Task<IActionResult> GetInventoryHistoryPdf([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.Set<Inventory>().AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.DateReceived >= from.Value);
            if (to.HasValue) q = q.Where(x => x.DateReceived <= to.Value);

            var items = await q.OrderByDescending(x => x.DateReceived).ToListAsync();
            var pdf = ReportPdfGenerator.GenerateInventoryReport(items);
            return File(pdf, "application/pdf", $"InventoryHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ===== Teardown history =====

        [HttpGet("teardown-history")]
        public async Task<IActionResult> GetTeardownHistory([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.TeardownBatches.AsNoTracking()
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .Include(b => b.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(b => b.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(b => b.DateProcessed >= from.Value);
            if (to.HasValue) q = q.Where(b => b.DateProcessed <= to.Value);

            return Ok(await q.OrderByDescending(b => b.DateProcessed).ToListAsync());
        }

        [HttpGet("teardown-history/pdf")]
        public async Task<IActionResult> GetTeardownHistoryPdf([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.TeardownBatches.AsNoTracking()
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .Include(b => b.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(b => b.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(b => b.DateProcessed >= from.Value);
            if (to.HasValue) q = q.Where(b => b.DateProcessed <= to.Value);

            var batches = await q.OrderByDescending(b => b.DateProcessed).ToListAsync();
            var pdf = ReportPdfGenerator.GenerateTeardownReport(batches);
            return File(pdf, "application/pdf", $"TeardownHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        // ===== Commodity sales history =====

        [HttpGet("commodity-sales-history")]
        public async Task<IActionResult> GetCommoditySalesHistory([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.Set<CommoditySale>().AsNoTracking()
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.SaleDate >= from.Value);
            if (to.HasValue) q = q.Where(x => x.SaleDate <= to.Value);

            return Ok(await q.OrderByDescending(x => x.SaleDate).ToListAsync());
        }

        [HttpGet("commodity-sales-history/pdf")]
        public async Task<IActionResult> GetCommoditySalesHistoryPdf([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.Set<CommoditySale>().AsNoTracking()
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.SaleDate >= from.Value);
            if (to.HasValue) q = q.Where(x => x.SaleDate <= to.Value);

            var sales = await q.OrderByDescending(x => x.SaleDate).ToListAsync();
            var pdf = ReportPdfGenerator.GenerateSalesReport(sales);
            return File(pdf, "application/pdf", $"SalesHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet("procurement-history")]
        public async Task<IActionResult> GetProcurementHistory([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.ProcurementRequests.AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.RequestedAtUtc >= from.Value);
            if (to.HasValue) q = q.Where(x => x.RequestedAtUtc <= to.Value);

            return Ok(await q.OrderByDescending(x => x.RequestedAtUtc).ToListAsync());
        }

        [HttpGet("procurement-history/pdf")]
        public async Task<IActionResult> GetProcurementHistoryPdf([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);
            var q = _db.ProcurementRequests.AsNoTracking()
                .Include(x => x.DeviceCategory)
                .Include(x => x.Branch)
                .AsQueryable();
            if (targetBranch.HasValue) q = q.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) q = q.Where(x => x.RequestedAtUtc >= from.Value);
            if (to.HasValue) q = q.Where(x => x.RequestedAtUtc <= to.Value);

            var items = await q.OrderByDescending(x => x.RequestedAtUtc).ToListAsync();
            var pdf = ReportPdfGenerator.GenerateProcurementReport(items);
            return File(pdf, "application/pdf", $"ProcurementHistory_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet("overall/pdf")]
        public async Task<IActionResult> GetOverallReportPdf([FromQuery] int? branchId = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        {
            var targetBranch = EffectiveBranchId(branchId);

            var invQ = _db.Set<Inventory>().AsNoTracking().Include(x => x.DeviceCategory).AsQueryable();
            if (targetBranch.HasValue) invQ = invQ.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) invQ = invQ.Where(x => x.DateReceived >= from.Value);
            if (to.HasValue) invQ = invQ.Where(x => x.DateReceived <= to.Value);
            var inventory = await invQ.OrderByDescending(x => x.DateReceived).ToListAsync();

            var tdQ = _db.TeardownBatches.AsNoTracking().Include(b => b.DeviceCategory).Include(b => b.Yields).AsQueryable();
            if (targetBranch.HasValue) tdQ = tdQ.Where(b => b.BranchId == targetBranch.Value);
            if (from.HasValue) tdQ = tdQ.Where(b => b.DateProcessed >= from.Value);
            if (to.HasValue) tdQ = tdQ.Where(b => b.DateProcessed <= to.Value);
            var teardowns = await tdQ.OrderByDescending(b => b.DateProcessed).ToListAsync();

            var salesQ = _db.Set<CommoditySale>().AsNoTracking().AsQueryable();
            if (targetBranch.HasValue) salesQ = salesQ.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) salesQ = salesQ.Where(x => x.SaleDate >= from.Value);
            if (to.HasValue) salesQ = salesQ.Where(x => x.SaleDate <= to.Value);
            var sales = await salesQ.OrderByDescending(x => x.SaleDate).ToListAsync();

            var procQ = _db.ProcurementRequests.AsNoTracking().Include(x => x.DeviceCategory).Include(x => x.Branch).AsQueryable();
            if (targetBranch.HasValue) procQ = procQ.Where(x => x.BranchId == targetBranch.Value);
            if (from.HasValue) procQ = procQ.Where(x => x.RequestedAtUtc >= from.Value);
            if (to.HasValue) procQ = procQ.Where(x => x.RequestedAtUtc <= to.Value);
            var procurement = await procQ.OrderByDescending(x => x.RequestedAtUtc).ToListAsync();

            var pdf = ReportPdfGenerator.GenerateOverallReport(inventory, teardowns, sales, procurement);
            return File(pdf, "application/pdf", $"OverallReport_{DateTime.Now:yyyyMMdd}.pdf");
        }
    }
}