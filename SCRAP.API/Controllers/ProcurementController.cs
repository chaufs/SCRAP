using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/procurement")]
    [Authorize]
    public class ProcurementController : ControllerBase
    {
        private readonly TenantErpDbContext _db;
        private readonly MasterErpDbContext? _masterDb;
        private readonly ITenantContext? _tenantContext;

        public ProcurementController(
            TenantErpDbContext db,
            MasterErpDbContext? masterDb = null,
            ITenantContext? tenantContext = null)
        {
            _db = db;
            _masterDb = masterDb;
            _tenantContext = tenantContext;
        }

        private async Task<bool> IsFinanceModuleEnabledAsync(bool? clientHasFinanceModule)
        {
            if (clientHasFinanceModule.HasValue)
                return clientHasFinanceModule.Value;

            if (_masterDb == null) return false;

            try
            {
                string? companyCode = _tenantContext?.CompanyCode;
                if (string.IsNullOrWhiteSpace(companyCode) && Request.Headers.TryGetValue("X-Company-Code", out var headerVal))
                {
                    companyCode = headerVal.FirstOrDefault();
                }

                Company? company = null;
                if (!string.IsNullOrWhiteSpace(companyCode))
                {
                    company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyCode == companyCode);
                }
                else if (_tenantContext?.CompanyId > 0)
                {
                    company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == _tenantContext.CompanyId);
                }

                if (company != null)
                {
                    var modules = !string.IsNullOrWhiteSpace(company.EnabledModules)
                        ? company.EnabledModules
                        : TenantsController.GetDefaultModulesForPlan(company.SubscriptionPlan);

                    return modules.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                                  .Any(m => string.Equals(m, "Finance", StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
                // In case of resolution error, do not block procurement
            }

            return false;
        }

        private int? CallerBranchId()
        {
            var val = User.FindFirstValue("BranchId");
            return int.TryParse(val, out var id) && id > 0 ? id : (int?)null;
        }

        private int? GetCurrentUserId()
        {
            var value = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(value, out var id) ? id : null;
        }

        private string GetCurrentUsername() => User.FindFirst(ClaimTypes.Name)?.Value ?? "unknown";

        private async Task<string> GetCurrentUserFullNameAsync()
        {
            var uid = GetCurrentUserId();
            if (uid.HasValue)
            {
                var u = await _db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == uid.Value);
                if (u != null)
                {
                    var name = $"{u.FirstName} {u.LastName}".Trim();
                    if (!string.IsNullOrWhiteSpace(name)) return name;
                }
            }
            return GetCurrentUsername();
        }

        private bool IsManagerOrAdmin() =>
            User.IsInRole("Manager") || User.IsInRole("Admin") || User.IsInRole("Superadmin");

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _tablesVerified = new(StringComparer.OrdinalIgnoreCase);

        private async Task EnsureTableCreatedAsync()
        {
            try
            {
                var conn = _db.Database.GetDbConnection().ConnectionString;
                if (!string.IsNullOrEmpty(conn) && _tablesVerified.ContainsKey(conn)) return;

                await _db.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcurementRequests')
                    BEGIN
                        CREATE TABLE [ProcurementRequests] (
                            [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
                            [SupplierCompany] NVARCHAR(300) NOT NULL,
                            [DeviceName] NVARCHAR(200) NOT NULL,
                            [DeviceCategoryId] INT NOT NULL,
                            [Quantity] INT NOT NULL DEFAULT 1,
                            [TotalCost] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [CostPerDevice] DECIMAL(18,2) NOT NULL DEFAULT 0,
                            [HasStorageDevice] BIT NOT NULL DEFAULT 0,
                            [SerialNumber] NVARCHAR(200) NULL,
                            [BatchCode] NVARCHAR(200) NULL,
                            [Notes] NVARCHAR(1000) NULL,
                            [Status] NVARCHAR(30) NOT NULL DEFAULT 'PendingApproval',
                            [RequestedByUserId] INT NULL,
                            [RequestedByUserName] NVARCHAR(100) NULL,
                            [RequestedByFullName] NVARCHAR(200) NULL,
                            [RequestedAtUtc] DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            [ReviewedByUserId] INT NULL,
                            [ReviewedByUserName] NVARCHAR(100) NULL,
                            [ReviewedByFullName] NVARCHAR(200) NULL,
                            [ReviewedAtUtc] DATETIME2 NULL,
                            [RejectionReason] NVARCHAR(500) NULL,
                            [AssignedTechStaffUserId] INT NULL,
                            [AssignedTechStaffUserName] NVARCHAR(100) NULL,
                            [AssignedTechStaffFullName] NVARCHAR(200) NULL,
                            [AssignedAtUtc] DATETIME2 NULL,
                            [CompletedAtUtc] DATETIME2 NULL,
                            [CompletedByUserName] NVARCHAR(100) NULL,
                            [BranchId] INT NOT NULL DEFAULT 1,
                            CONSTRAINT [FK_ProcurementRequests_DeviceCategories_DeviceCategoryId] FOREIGN KEY ([DeviceCategoryId]) REFERENCES [DeviceCategories] ([Id]),
                            CONSTRAINT [FK_ProcurementRequests_Branches_BranchId] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([Id])
                        );
                    END
                ");
                if (!string.IsNullOrEmpty(conn)) _tablesVerified[conn] = true;
            }
            catch
            {
                // Silently continue if concurrent creation or already exists
            }
        }

        // 1. Submit a procurement request (Sales Staff, Manager, Admin)
        [HttpPost("request")]
        public async Task<ActionResult<ProcurementRequest>> CreateRequest(CreateProcurementRequestDto request)
        {
            await EnsureTableCreatedAsync();
            if (string.IsNullOrWhiteSpace(request.SupplierCompany))
                return BadRequest("Supplier or source company name is required.");
            if (string.IsNullOrWhiteSpace(request.DeviceName))
                return BadRequest("Device name/type is required.");
            if (request.DeviceCategoryId <= 0 || !await _db.DeviceCategories.AnyAsync(x => x.Id == request.DeviceCategoryId))
                return BadRequest("A valid device category is required.");
            if (request.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");
            if (request.Quantity > 10000)
                return BadRequest("Quantity cannot exceed 10,000 devices per procurement.");
            if (request.TotalCost <= 0)
                return BadRequest("Total procurement cost must be greater than zero.");

            var branchId = CallerBranchId() ?? request.BranchId ?? 1;
            var totalCost = Math.Round(request.TotalCost, 2, MidpointRounding.AwayFromZero);
            var costPerDevice = Math.Round(totalCost / request.Quantity, 2, MidpointRounding.AwayFromZero);

            var requesterUserId = GetCurrentUserId();
            var requesterUsername = GetCurrentUsername();
            var requesterFullName = await GetCurrentUserFullNameAsync();

            string? serial = request.SerialNumber?.Trim();
            string? batch = request.BatchCode?.Trim();

            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            if (request.Quantity <= 1)
            {
                if (string.IsNullOrWhiteSpace(serial) || serial.StartsWith("BAT-") || serial.Contains("auto on intake"))
                {
                    var suffix = new string(Enumerable.Repeat(chars, 4).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
                    serial = $"SN-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
                }
                batch = null;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(batch))
                {
                    var suffix = new string(Enumerable.Repeat(chars, 4).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
                    batch = $"BAT-{DateTime.UtcNow:yyyyMMdd}-{suffix}";
                }
                serial = null;
            }

            var procRequest = new ProcurementRequest
            {
                SupplierCompany = request.SupplierCompany.Trim(),
                DeviceName = request.DeviceName.Trim(),
                DeviceCategoryId = request.DeviceCategoryId,
                Quantity = request.Quantity,
                TotalCost = totalCost,
                CostPerDevice = costPerDevice,
                HasStorageDevice = request.HasStorageDevice,
                SerialNumber = serial,
                BatchCode = batch,
                Notes = request.Notes?.Trim(),
                Status = ProcurementStatus.PendingApproval,
                RequestedByUserId = requesterUserId,
                RequestedByUserName = requesterUsername,
                RequestedByFullName = requesterFullName,
                RequestedAtUtc = DateTime.UtcNow,
                BranchId = branchId
            };

            _db.ProcurementRequests.Add(procRequest);
            await _db.SaveChangesAsync();

            // Include category in response
            await _db.Entry(procRequest).Reference(p => p.DeviceCategory).LoadAsync();

            return Ok(procRequest);
        }

        // Backward compatibility for direct POST api/procurement
        [HttpPost]
        public async Task<ActionResult<ProcurementRequest>> Create(CreateProcurementRequestDto request)
        {
            return await CreateRequest(request);
        }

        // 2. Get procurement requests with filters
        [HttpGet("requests")]
        public async Task<ActionResult<List<ProcurementRequest>>> GetRequests(
            [FromQuery] string? status = null,
            [FromQuery] int? branchId = null,
            [FromQuery] bool? myAssignedOnly = null)
        {
            await EnsureTableCreatedAsync();
            var q = _db.ProcurementRequests
                .Include(p => p.DeviceCategory)
                .Include(p => p.Branch)
                .AsQueryable();

            var currentUserId = GetCurrentUserId();

            // Tech staff only sees requests assigned to them if myAssignedOnly is true or if they are in TechStaff role
            if (User.IsInRole("TechStaff") && !IsManagerOrAdmin())
            {
                q = q.Where(p => p.AssignedTechStaffUserId == currentUserId && p.Status == ProcurementStatus.Approved);
            }
            else if (myAssignedOnly == true && currentUserId.HasValue)
            {
                q = q.Where(p => p.AssignedTechStaffUserId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (Enum.TryParse<ProcurementStatus>(status, true, out var parsedStatus))
                {
                    q = q.Where(p => p.Status == parsedStatus);
                }
            }

            var effBranch = CallerBranchId() ?? branchId;
            if (effBranch.HasValue && effBranch.Value > 0 && !User.IsInRole("Superadmin"))
            {
                q = q.Where(p => p.BranchId == effBranch.Value);
            }

            var list = await q.OrderByDescending(p => p.RequestedAtUtc).ToListAsync();
            return Ok(list);
        }

        // 2b. Get single procurement request by ID
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ProcurementRequest>> GetRequestById(int id)
        {
            await EnsureTableCreatedAsync();
            var item = await _db.ProcurementRequests
                .Include(p => p.DeviceCategory)
                .Include(p => p.Branch)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);

            if (item == null)
                return NotFound($"Procurement request #{id} not found.");

            return Ok(item);
        }

        // 3. Manager reviews and accepts or rejects request
        [HttpPost("{id:int}/review")]
        public async Task<ActionResult<ProcurementRequest>> ReviewRequest(int id, [FromBody] ReviewProcurementDto dto)
        {
            await EnsureTableCreatedAsync();
            if (!IsManagerOrAdmin())
                return Forbid("Only Managers or Administrators can review and accept procurement requests.");

            var proc = await _db.ProcurementRequests
                .Include(p => p.DeviceCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proc == null)
                return NotFound($"Procurement request #{id} not found.");

            if (proc.Status != ProcurementStatus.PendingApproval)
                return BadRequest($"Procurement request #{id} is not in pending approval status (current status: {proc.Status}).");

            var managerUserId = GetCurrentUserId();
            var managerUsername = GetCurrentUsername();
            var managerFullName = await GetCurrentUserFullNameAsync();

            if (dto.Accept)
            {
                if (!dto.AssignedTechStaffUserId.HasValue || dto.AssignedTechStaffUserId.Value <= 0)
                    return BadRequest("You must assign a Tech Staff member to handle the arrival of the devices.");

                var techUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.AssignedTechStaffUserId.Value && u.IsActive);
                if (techUser == null)
                    return BadRequest("The selected Tech Staff member was not found or is inactive.");

                bool hasFinanceModule = await IsFinanceModuleEnabledAsync(dto.HasFinanceModule);

                if (hasFinanceModule)
                {
                    // Check branch funds only if the company's plan has the Finance module
                    var totalIncome = await _db.CompanyFinanceTransactions
                        .Where(x => x.BranchId == proc.BranchId && x.Type == FinanceTransactionType.Income)
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    var totalDeductions = await _db.CompanyFinanceTransactions
                        .Where(x => x.BranchId == proc.BranchId && x.Type == FinanceTransactionType.Deduction)
                        .SumAsync(x => (decimal?)x.Amount) ?? 0m;
                    var availableBalance = totalIncome - totalDeductions;

                    if (proc.TotalCost > availableBalance)
                        return BadRequest($"Insufficient branch funds. Available balance: {availableBalance:0.00}; procurement cost: {proc.TotalCost:0.00}.");
                }

                // Assign tech staff
                proc.AssignedTechStaffUserId = techUser.Id;
                proc.AssignedTechStaffUserName = techUser.Username;
                proc.AssignedTechStaffFullName = $"{techUser.FirstName} {techUser.LastName}".Trim();
                proc.AssignedAtUtc = DateTime.UtcNow;

                // Record acceptance by manager
                proc.ReviewedByUserId = managerUserId;
                proc.ReviewedByUserName = managerUsername;
                proc.ReviewedByFullName = managerFullName;
                proc.ReviewedAtUtc = DateTime.UtcNow;
                proc.Status = ProcurementStatus.Approved;

                if (hasFinanceModule)
                {
                    // Deduct from branch finance only if company subscription plan includes the Finance module
                    _db.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
                    {
                        Type = FinanceTransactionType.Deduction,
                        Category = "Device Procurement",
                        Amount = proc.TotalCost,
                        TransactionDate = DateTime.Today,
                        Description = $"Procurement of {proc.Quantity} {proc.DeviceName} from {proc.SupplierCompany} (Approved by {managerFullName}, Assigned to {proc.AssignedTechStaffFullName})",
                        SourceReference = $"ProcurementReq:{proc.Id}:{Guid.NewGuid():N}",
                        RecordedByUserId = managerUserId,
                        BranchId = proc.BranchId
                    });
                }
            }
            else
            {
                // Rejected by manager
                proc.Status = ProcurementStatus.Rejected;
                proc.RejectionReason = string.IsNullOrWhiteSpace(dto.RejectionReason)
                    ? "Rejected by manager"
                    : dto.RejectionReason.Trim();
                proc.ReviewedByUserId = managerUserId;
                proc.ReviewedByUserName = managerUsername;
                proc.ReviewedByFullName = managerFullName;
                proc.ReviewedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();
            return Ok(proc);
        }

        // 4. Tech Staff handles arrival and puts devices into inventory (Task Completed)
        [HttpPost("{id:int}/complete-arrival")]
        public async Task<ActionResult> CompleteArrival(int id)
        {
            await EnsureTableCreatedAsync();
            var proc = await _db.ProcurementRequests
                .Include(p => p.DeviceCategory)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (proc == null)
                return NotFound($"Procurement request #{id} not found.");

            if (proc.Status != ProcurementStatus.Approved)
                return BadRequest($"Procurement request #{id} is not in approved status waiting for arrival (current status: {proc.Status}).");

            var currentUserId = GetCurrentUserId();
            var currentUsername = GetCurrentUsername();
            var currentFullName = await GetCurrentUserFullNameAsync();

            // Verify authorization if user is TechStaff
            if (User.IsInRole("TechStaff") && proc.AssignedTechStaffUserId.HasValue && proc.AssignedTechStaffUserId.Value != currentUserId)
            {
                // Still allow if same branch or manager/admin
                if (!IsManagerOrAdmin())
                    return Forbid("This procurement task is assigned to a different Tech Staff member.");
            }

            // Put devices into Inventory
            var devices = new List<Inventory>(proc.Quantity);
            var today = DateTime.Today;

            if (proc.Quantity <= 1)
            {
                var serial = !string.IsNullOrWhiteSpace(proc.SerialNumber)
                    ? proc.SerialNumber.Trim()
                    : $"SN-{today:yyyyMMdd}-{proc.Id:D4}";

                devices.Add(new Inventory
                {
                    DeviceName = proc.DeviceName.Trim(),
                    DeviceCategoryId = proc.DeviceCategoryId,
                    SerialNumber = serial,
                    BatchCode = proc.BatchCode?.Trim(),
                    Status = InventoryStatus.InStock,
                    DateReceived = today,
                    PurchaseDate = proc.RequestedAtUtc.Date,
                    PurchasedFrom = proc.SupplierCompany.Trim(),
                    PurchaseCost = proc.CostPerDevice,
                    ProcurementQuantity = proc.Quantity,
                    HasStorageDevice = proc.HasStorageDevice,
                    Notes = proc.Notes?.Trim(),
                    RecordedByUsername = currentFullName,
                    BranchId = proc.BranchId
                });
            }
            else
            {
                var batchCode = !string.IsNullOrWhiteSpace(proc.BatchCode)
                    ? proc.BatchCode.Trim()
                    : $"BAT-{today:yyyyMMdd}-{proc.Id:D4}";

                for (var index = 0; index < proc.Quantity; index++)
                {
                    var serial = $"{batchCode}-{(index + 1):D3}";

                    devices.Add(new Inventory
                    {
                        DeviceName = proc.DeviceName.Trim(),
                        DeviceCategoryId = proc.DeviceCategoryId,
                        SerialNumber = serial,
                        BatchCode = batchCode,
                        Status = InventoryStatus.InStock,
                        DateReceived = today,
                        PurchaseDate = proc.RequestedAtUtc.Date,
                        PurchasedFrom = proc.SupplierCompany.Trim(),
                        PurchaseCost = proc.CostPerDevice,
                        ProcurementQuantity = proc.Quantity,
                        HasStorageDevice = proc.HasStorageDevice,
                        Notes = proc.Notes?.Trim(),
                        RecordedByUsername = currentFullName,
                        BranchId = proc.BranchId
                    });
                }
            }

            _db.Inventories.AddRange(devices);

            proc.Status = ProcurementStatus.Completed;
            proc.CompletedAtUtc = DateTime.UtcNow;
            proc.CompletedByUserName = currentFullName;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                message = "Task completed successfully. Devices have been put into inventory.",
                procurementId = proc.Id,
                devicesAdded = devices.Count,
                inventoryIds = devices.Select(d => d.Id).ToList()
            });
        }

        // 5. Get list of active tech staff members for manager assignment
        [HttpGet("tech-staff")]
        public async Task<ActionResult<List<TechStaffUserDto>>> GetTechStaff()
        {
            var branchId = CallerBranchId();
            var q = _db.Users.AsNoTracking().Where(u => u.IsActive && u.Role == UserRole.TechStaff);

            if (branchId.HasValue && branchId.Value > 0 && !User.IsInRole("Superadmin"))
            {
                q = q.Where(u => u.BranchId == branchId.Value || u.BranchId == null);
            }

            var techUsers = await q
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Select(u => new TechStaffUserDto
                {
                    Id = u.Id,
                    Username = u.Username,
                    FullName = (u.FirstName + " " + u.LastName).Trim(),
                    BranchId = u.BranchId
                })
                .ToListAsync();

            // Deduplicate if redundant alias or duplicate accounts exist for the same staff member
            var deduplicated = techUsers
                .GroupBy(u => string.IsNullOrWhiteSpace(u.FullName) ? u.Username.ToLowerInvariant() : u.FullName.ToLowerInvariant())
                .Select(g => g
                    .OrderByDescending(u => u.Username.Contains('.'))
                    .ThenBy(u => u.Id)
                    .First())
                .OrderBy(u => u.FullName)
                .ToList();

            return Ok(deduplicated);
        }

        // 6. Procurement history endpoint showing all 3 personnel names
        [HttpGet("history")]
        public async Task<ActionResult<List<ProcurementHistoryDto>>> GetHistory([FromQuery] int? branchId = null)
        {
            await EnsureTableCreatedAsync();
            var targetBranch = CallerBranchId() ?? branchId;
            var q = _db.ProcurementRequests
                .Include(p => p.DeviceCategory)
                .Include(p => p.Branch)
                .AsNoTracking()
                .AsQueryable();

            if (targetBranch.HasValue && targetBranch.Value > 0 && !User.IsInRole("Superadmin"))
            {
                q = q.Where(p => p.BranchId == targetBranch.Value);
            }

            var list = await q
                .OrderByDescending(p => p.RequestedAtUtc)
                .Select(p => new ProcurementHistoryDto
                {
                    Id = p.Id,
                    RequestedDate = p.RequestedAtUtc,
                    SupplierCompany = p.SupplierCompany,
                    DeviceName = p.DeviceName,
                    CategoryName = p.DeviceCategory != null ? p.DeviceCategory.Name : "—",
                    Quantity = p.Quantity,
                    TotalCost = p.TotalCost,
                    CostPerDevice = p.CostPerDevice,
                    Status = p.Status.ToString(),
                    RequestedByFullName = p.RequestedByFullName ?? p.RequestedByUserName ?? "—",
                    ReviewedByFullName = p.ReviewedByFullName ?? p.ReviewedByUserName ?? "—",
                    AssignedTechStaffFullName = p.AssignedTechStaffFullName ?? p.AssignedTechStaffUserName ?? "—",
                    SerialNumber = !string.IsNullOrWhiteSpace(p.SerialNumber) ? p.SerialNumber : (p.BatchCode ?? "—"),
                    BatchCode = p.BatchCode,
                    Notes = p.Notes ?? "",
                    RejectionReason = p.RejectionReason,
                    CompletedAtUtc = p.CompletedAtUtc,
                    BranchName = p.Branch != null ? p.Branch.Name : ""
                })
                .ToListAsync();

            return Ok(list);
        }

        public class CreateProcurementRequestDto
        {
            public string SupplierCompany { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public int DeviceCategoryId { get; set; }
            public int Quantity { get; set; } = 1;
            public decimal TotalCost { get; set; }
            public bool HasStorageDevice { get; set; }
            public string? SerialNumber { get; set; }
            public string? BatchCode { get; set; }
            public string? Notes { get; set; }
            public int? BranchId { get; set; }
        }

        public class ReviewProcurementDto
        {
            public bool Accept { get; set; }
            public int? AssignedTechStaffUserId { get; set; }
            public int? AssignedTechStaffId
            {
                get => AssignedTechStaffUserId;
                set => AssignedTechStaffUserId = value;
            }
            public string? RejectionReason { get; set; }
            public bool? HasFinanceModule { get; set; }
        }

        public class TechStaffUserDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public int? BranchId { get; set; }
        }

        public class ProcurementHistoryDto
        {
            public int Id { get; set; }
            public DateTime RequestedDate { get; set; }
            public string SupplierCompany { get; set; } = string.Empty;
            public string DeviceName { get; set; } = string.Empty;
            public string CategoryName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal TotalCost { get; set; }
            public decimal CostPerDevice { get; set; }
            public string Status { get; set; } = string.Empty;
            public string RequestedByFullName { get; set; } = string.Empty;
            public string ReviewedByFullName { get; set; } = string.Empty;
            public string AssignedTechStaffFullName { get; set; } = string.Empty;
            public string SerialNumber { get; set; } = string.Empty;
            public string? BatchCode { get; set; }
            public string Notes { get; set; } = string.Empty;
            public string? RejectionReason { get; set; }
            public DateTime? CompletedAtUtc { get; set; }
            public string BranchName { get; set; } = string.Empty;
        }
    }
}
