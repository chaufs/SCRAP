using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.infrastructure.data;
using SCRAP.domain.entities;
using System.Security.Claims;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TeardownController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        public TeardownController(TenantErpDbContext db) => _db = db;

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) ? id : (int?)null;
        }
        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("Superadmin");

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Teardown>>> GetAll() => await _db.Teardowns.Include(t => t.InventoryItem).ToListAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Teardown>> Get(int id)
        {
            var item = await _db.Teardowns.Include(t => t.InventoryItem).FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return NotFound();
            return item;
        }

        [HttpGet("stock-check")]
        public async Task<ActionResult> CheckStock([FromQuery] int categoryId, [FromQuery] int quantity = 1)
        {
            var category = await _db.DeviceCategories.FindAsync(categoryId);
            if (category == null) return NotFound(new Models.ErrorResponse { Message = "Category not found." });

            var inventoryQ = _db.Set<Inventory>()
                .Where(i => i.DeviceCategoryId == categoryId && i.Status == InventoryStatus.InStock);

            if (!IsAdmin())
            {
                var callerBranchId = CallerBranchId() ?? -1;
                inventoryQ = inventoryQ.Where(i => i.BranchId == callerBranchId);
            }

            var inStockCount = await inventoryQ.CountAsync();
            bool hasSufficientStock = inStockCount >= quantity && quantity > 0;

            return Ok(new
            {
                categoryId,
                categoryName = category.Name,
                availableStock = inStockCount,
                requestedQuantity = quantity,
                hasSufficientStock
            });
        }

        [HttpPost]
        //[Microsoft.AspNetCore.Authorization.Authorize(Policy = "RequireTech")]
        public async Task<ActionResult> CreateBatch([FromBody] TeardownBatchRequest request)
        {
            // Basic validation
            if (request == null) return BadRequest(new Models.ErrorResponse { Message = "Request body is required" });
            if (request.DeviceCategoryId == 0) return BadRequest(new Models.ErrorResponse { Message = "DeviceCategoryId is required" });
            if (request.Quantity <= 0) return BadRequest(new Models.ErrorResponse { Message = "Quantity must be greater than zero" });

            // load archetype recipes for the category
            var recipes = await _db.ArchetypeRecipes.Where(r => r.DeviceCategoryId == request.DeviceCategoryId).ToListAsync();
            if (recipes == null || recipes.Count == 0) return BadRequest(new Models.ErrorResponse { Message = "No archetype recipes defined for that device category" });

            // check enough devices are available in inventory (scoped to caller's branch)
            var inventoryQ = _db.Set<Inventory>()
                .Where(i => i.DeviceCategoryId == request.DeviceCategoryId && i.Status == InventoryStatus.InStock);

            if (!IsAdmin())
            {
                var callerBranchId = CallerBranchId() ?? -1;
                inventoryQ = inventoryQ.Where(i => i.BranchId == callerBranchId);
            }

            var availableDevices = await inventoryQ
                .OrderBy(i => i.DateReceived)
                .Take(request.Quantity)
                .ToListAsync();

            if (availableDevices.Count < request.Quantity)
                return BadRequest(new Models.ErrorResponse { Message = $"Only {availableDevices.Count} device(s) available in this category, but {request.Quantity} requested." });

            // compute yields
            var overrideDict = request.Overrides != null
                ? new Dictionary<string, decimal>(request.Overrides, StringComparer.OrdinalIgnoreCase)
                : null;

            var yields = new List<TeardownYield>();
            foreach (var r in recipes)
            {
                var expected = r.WeightKgPerUnit * request.Quantity;
                var finalWeight = overrideDict != null && overrideDict.TryGetValue(r.MaterialName, out var over)
                    ? Math.Max(0, over)
                    : expected;

                yields.Add(new TeardownYield { MaterialName = r.MaterialName, WeightKg = finalWeight });
            }

            // transaction: create batch, decrement inventory, queue storage destruction, update raw inventory totals
            using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var callerBranch = CallerBranchId();
                var branchId = callerBranch ?? (IsAdmin() ? (availableDevices.FirstOrDefault()?.BranchId ?? 1) : 0);
                if (branchId == 0) return BadRequest(new Models.ErrorResponse { Message = "User has no branch assigned." });

                int processedById = request.ProcessedByUserId > 0 
                    ? request.ProcessedByUserId 
                    : (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var uid) ? uid : 1);

                var batch = new TeardownBatch
                {
                    ProcessedByUserId  = processedById,
                    DeviceCategoryId   = request.DeviceCategoryId,
                    QuantityDismantled = request.Quantity,
                    DateProcessed      = DateTime.UtcNow,
                    BranchId           = branchId
                };
                _db.TeardownBatches.Add(batch);
                await _db.SaveChangesAsync();

                // mark the devices as disposed and queue storage destruction where applicable
                foreach (var device in availableDevices)
                {
                    device.Status = InventoryStatus.Disposed;

                    if (device.HasStorageDevice)
                    {
                        _db.Add(new StorageDestructionRecord
                        {
                            InventoryId = device.Id,
                            TeardownBatchId = batch.Id
                        });
                    }
                }

                // attach yields to the batch and upsert raw inventory
                foreach (var y in yields)
                {
                    y.TeardownBatchId = batch.Id;
                    _db.TeardownYields.Add(y);

                    var inv = await _db.RawInventories.FirstOrDefaultAsync(r => r.MaterialName == y.MaterialName);
                    if (inv == null)
                    {
                        inv = new RawInventory { MaterialName = y.MaterialName, CurrentTotalWeightKg = y.WeightKg };
                        _db.RawInventories.Add(inv);
                    }
                    else
                    {
                        inv.CurrentTotalWeightKg += y.WeightKg;
                        _db.Entry(inv).State = EntityState.Modified;
                    }
                }

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                // return created batch summary
                var result = new { BatchId = batch.Id, Yields = yields };
                return CreatedAtAction(nameof(GetAll), result);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync();
                return BadRequest(new Models.ErrorResponse { Message = "Database update error", Detail = ex.InnerException?.Message ?? ex.Message });
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return StatusCode(500, new Models.ErrorResponse { Message = "Unexpected error", Detail = ex.Message });
            }
        }

        public class TeardownBatchRequest
        {
            public int ProcessedByUserId { get; set; }
            public int DeviceCategoryId { get; set; }
            public int Quantity { get; set; }
            // optional overrides by material name
            public Dictionary<string, decimal>? Overrides { get; set; }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, Teardown model)
        {
            if (id != model.Id) return BadRequest();
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _db.Teardowns.FindAsync(id);
            if (item == null) return NotFound();
            _db.Teardowns.Remove(item);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("history")]
        public async Task<ActionResult<IEnumerable<TeardownBatch>>> GetBatchHistory([FromQuery] int? branchId = null)
        {
            var q = _db.TeardownBatches
                .Include(b => b.DeviceCategory)
                .Include(b => b.Yields)
                .Include(b => b.Branch)
                .AsQueryable();

            if (!IsAdmin())
            {
                var userBranchId = CallerBranchId() ?? -1;
                q = q.Where(b => b.BranchId == userBranchId);
            }
            else if (branchId.HasValue && branchId.Value > 0)
            {
                q = q.Where(b => b.BranchId == branchId.Value);
            }

            return Ok(await q.OrderByDescending(b => b.DateProcessed).ToListAsync());
        }
    }
}