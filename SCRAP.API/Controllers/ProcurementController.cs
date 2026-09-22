using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/procurement")]
    [Authorize(Policy = "RequireManager")]
    public class ProcurementController : ControllerBase
    {
        private readonly MasterErpDbContext _db;

        public ProcurementController(MasterErpDbContext db) => _db = db;

        [HttpPost]
        public async Task<ActionResult<ProcurementResult>> Create(ProcurementRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.DeviceName)) return BadRequest("Device name is required.");
            if (request.DeviceCategoryId <= 0 || !await _db.DeviceCategories.AnyAsync(x => x.Id == request.DeviceCategoryId))
                return BadRequest("A valid device category is required.");
            if (request.Quantity <= 0) return BadRequest("Quantity must be greater than zero.");
            if (request.Quantity > 10000) return BadRequest("Quantity cannot exceed 10,000 devices per procurement.");
            if (request.TotalCost <= 0) return BadRequest("Total purchase cost must be greater than zero.");
            if (string.IsNullOrWhiteSpace(request.PurchasedFrom)) return BadRequest("Purchased from is required.");
            if (request.Quantity > 1 && string.IsNullOrWhiteSpace(request.BatchCode))
                return BadRequest("A batch code is required when purchasing multiple devices.");
            if (request.Quantity == 1 && string.IsNullOrWhiteSpace(request.SerialNumber) && string.IsNullOrWhiteSpace(request.BatchCode))
                return BadRequest("Enter a serial number or batch code for the device.");

            var purchaseDate = (request.PurchaseDate ?? DateTime.Today).Date;
            var totalCost = Math.Round(request.TotalCost, 2, MidpointRounding.AwayFromZero);
            var totalIncome = await _db.CompanyFinanceTransactions
                .Where(x => x.Type == FinanceTransactionType.Income)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var totalDeductions = await _db.CompanyFinanceTransactions
                .Where(x => x.Type == FinanceTransactionType.Deduction)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;
            var availableBalance = totalIncome - totalDeductions;

            if (totalCost > availableBalance)
                return BadRequest($"Insufficient company funds. Available balance: {availableBalance:0.00}; procurement cost: {totalCost:0.00}.");

            var costPerDevice = request.Quantity == 0 ? 0 : Math.Round(request.TotalCost / request.Quantity, 2, MidpointRounding.AwayFromZero);
            var devices = new List<Inventory>(request.Quantity);

            for (var index = 0; index < request.Quantity; index++)
            {
                var code = request.Quantity == 1 && !string.IsNullOrWhiteSpace(request.SerialNumber)
                    ? request.SerialNumber.Trim()
                    : request.BatchCode!.Trim();

                devices.Add(new Inventory
                {
                    DeviceName = request.DeviceName.Trim(),
                    DeviceCategoryId = request.DeviceCategoryId,
                    SerialNumber = code,
                    BatchCode = request.BatchCode?.Trim(),
                    Status = InventoryStatus.InStock,
                    DateReceived = purchaseDate,
                    PurchaseDate = purchaseDate,
                    PurchasedFrom = request.PurchasedFrom.Trim(),
                    PurchaseCost = costPerDevice,
                    ProcurementQuantity = request.Quantity,
                    HasStorageDevice = request.HasStorageDevice,
                    Notes = request.Notes?.Trim()
                });
            }

            _db.Inventories.AddRange(devices);
            _db.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
            {
                Type = FinanceTransactionType.Deduction,
                Category = "Device Procurement",
                Amount = totalCost,
                TransactionDate = purchaseDate,
                Description = $"Procurement of {request.Quantity} {request.DeviceName.Trim()} from {request.PurchasedFrom.Trim()}",
                SourceReference = $"Procurement:{Guid.NewGuid():N}",
                RecordedByUserId = GetCurrentUserId()
            });
            await _db.SaveChangesAsync();

            return Ok(new ProcurementResult
            {
                Quantity = request.Quantity,
                TotalCost = totalCost,
                CostPerDevice = costPerDevice,
                BatchCode = request.BatchCode,
                InventoryIds = devices.Select(x => x.Id).ToArray()
            });
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        public sealed class ProcurementRequest
        {
            public string DeviceName { get; set; } = string.Empty;
            public int DeviceCategoryId { get; set; }
            public DateTime? PurchaseDate { get; set; }
            public string PurchasedFrom { get; set; } = string.Empty;
            public decimal TotalCost { get; set; }
            public int Quantity { get; set; } = 1;
            public bool HasStorageDevice { get; set; }
            public string? SerialNumber { get; set; }
            public string? BatchCode { get; set; }
            public string? Notes { get; set; }
        }

        public sealed class ProcurementResult
        {
            public int Quantity { get; set; }
            public decimal TotalCost { get; set; }
            public decimal CostPerDevice { get; set; }
            public string? BatchCode { get; set; }
            public IReadOnlyCollection<int> InventoryIds { get; set; } = Array.Empty<int>();
        }
    }
}
