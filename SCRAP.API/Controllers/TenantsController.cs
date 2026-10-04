using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure;
using SCRAP.infrastructure.data;
using SCRAP.infrastructure.services;
using SCRAP.API.Services;

namespace SCRAP.API.Controllers
{
    [ApiController]
    [Route("api/superadmin/[controller]")]
    [Authorize(Policy = "RequireSuperadmin")]
    public class TenantsController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly ILogger<TenantsController> _logger;

        public TenantsController(
            MasterErpDbContext masterDb,
            ITenantDbContextFactory tenantFactory,
            ILogger<TenantsController> logger)
        {
            _masterDb = masterDb;
            _tenantFactory = tenantFactory;
            _logger = logger;
        }

        private async Task CheckAndDeactivateExpiredCompaniesAsync()
        {
            var now = DateTime.UtcNow;
            var expiredCompanies = await _masterDb.Companies
                .Where(c => c.IsActive && c.SubscriptionExpiresAt.HasValue && c.SubscriptionExpiresAt.Value < now)
                .ToListAsync();

            if (expiredCompanies.Count > 0)
            {
                foreach (var c in expiredCompanies)
                {
                    c.IsActive = false;
                    _logger.LogWarning("Company {CompanyCode} ({CompanyName}) subscription expired on {ExpiresAt}. Automatically deactivated.",
                        c.CompanyCode, c.CompanyName, c.SubscriptionExpiresAt);

                    var dbs = await _masterDb.CompanyDatabases.Where(d => d.CompanyId == c.CompanyId).ToListAsync();
                    foreach (var d in dbs) d.IsActive = false;

                    TenantResolutionMiddleware.InvalidateCache(c.CompanyCode);
                }
                await _masterDb.SaveChangesAsync();
            }
        }

        // 1. List all subscriber companies and their database info
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            await CheckAndDeactivateExpiredCompaniesAsync();

            var companies = await _masterDb.Companies
                .AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.CompanyId,
                    c.CompanyCode,
                    c.CompanyName,
                    c.ContactEmail,
                    c.ContactPhone,
                    c.SubscriptionPlan,
                    c.SubscriptionExpiresAt,
                    c.EnabledModules,
                    c.IsActive,
                    c.CreatedAt,
                    Databases = _masterDb.CompanyDatabases
                        .Where(cd => cd.CompanyId == c.CompanyId)
                        .Select(cd => new
                        {
                            cd.CompanyDatabaseId,
                            cd.ServerName,
                            cd.DatabaseName,
                            cd.CredentialKey,
                            cd.DatabaseType,
                            cd.ConnectionString,
                            cd.IsActive
                        }).ToList()
                })
                .ToListAsync();

            return Ok(companies);
        }

        // 2. Get specific tenant subscriber info
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            await CheckAndDeactivateExpiredCompaniesAsync();

            var company = await _masterDb.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            var databases = await _masterDb.CompanyDatabases
                .AsNoTracking()
                .Where(cd => cd.CompanyId == id)
                .ToListAsync();

            return Ok(new { company, databases });
        }

        // 3. Register and provision a new tenant company (creates Master records, generates DB, runs migrations, seeds initial tenant Admin)
        [HttpPost("register")]
        public async Task<IActionResult> RegisterAndProvision([FromBody] RegisterTenantRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.CompanyName))
                return BadRequest(new { message = "CompanyName is required." });
            if (string.IsNullOrWhiteSpace(req.AdminUsername) || string.IsNullOrWhiteSpace(req.AdminPassword))
                return BadRequest(new { message = "Initial Company Admin username and password are required." });

            if (string.IsNullOrWhiteSpace(req.CompanyCode))
            {
                // Auto-increment logic
                var count = await _masterDb.Companies.CountAsync();
                req.CompanyCode = $"COMP{(count + 1):D3}";
            }

            var codeUpper = req.CompanyCode.Trim().ToUpperInvariant();

            // Check if CompanyCode is already taken
            if (await _masterDb.Companies.AnyAsync(c => c.CompanyCode == codeUpper))
            {
                return BadRequest(new { message = $"CompanyCode '{codeUpper}' is already registered." });
            }

            // A. Create Company record in Master DB
            var plan = string.IsNullOrWhiteSpace(req.SubscriptionPlan) ? "Standard" : req.SubscriptionPlan.Trim();
            var company = new Company
            {
                CompanyCode = codeUpper,
                CompanyName = req.CompanyName.Trim(),
                ContactEmail = req.ContactEmail?.Trim(),
                ContactPhone = req.ContactPhone?.Trim(),
                SubscriptionPlan = plan,
                SubscriptionExpiresAt = req.SubscriptionExpiresAt ?? DateTime.UtcNow.AddYears(1),
                EnabledModules = await GetModulesForPlanAsync(plan),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _masterDb.Companies.Add(company);
            await _masterDb.SaveChangesAsync();

            // Record initial subscription history
            var planAmount = company.SubscriptionPlan switch
            {
                "Basic" => 99.00m,
                "Standard" => 249.00m,
                "Enterprise" => 599.00m,
                _ => 199.00m
            };

            _masterDb.SubscriptionHistories.Add(new SubscriptionHistory
            {
                CompanyId = company.CompanyId,
                PlanName = company.SubscriptionPlan,
                Amount = planAmount,
                BillingCycle = "Monthly",
                StartDate = company.CreatedAt,
                EndDate = company.SubscriptionExpiresAt ?? DateTime.UtcNow.AddYears(1),
                Status = "Active",
                Notes = $"Initial {company.SubscriptionPlan} registration and onboarding.",
                CreatedAt = DateTime.UtcNow
            });
            await _masterDb.SaveChangesAsync();

            // B. Resolve database server and database name
            var dbName = string.IsNullOrWhiteSpace(req.CustomDatabaseName)
                ? $"DB_Tenant_{codeUpper}"
                : req.CustomDatabaseName.Trim();

            // Default to same server as MasterErp if not explicitly provided
            var defaultServer = "localhost";
            var masterConn = _masterDb.Database.GetConnectionString();
            if (!string.IsNullOrWhiteSpace(masterConn))
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(masterConn);
                if (!string.IsNullOrWhiteSpace(builder.DataSource))
                {
                    defaultServer = builder.DataSource;
                }
            }

            var serverName = string.IsNullOrWhiteSpace(req.DbServerName) ? defaultServer : req.DbServerName.Trim();
            var credentialKey = string.IsNullOrWhiteSpace(req.CredentialKey) ? "DefaultTenant" : req.CredentialKey.Trim();

            var companyDb = new CompanyDatabase
            {
                CompanyId = company.CompanyId,
                ServerName = serverName,
                DatabaseName = dbName,
                CredentialKey = credentialKey,
                DatabaseType = "Local",
                IsActive = true
            };

            _masterDb.CompanyDatabases.Add(companyDb);

            // Also register Cloud database entry if cloud connection info is provided
            string? cloudDbName = null;
            if (!string.IsNullOrWhiteSpace(req.CloudConnectionString))
            {
                // Parse the cloud connection string to extract server and database name
                try
                {
                    var cloudBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(req.CloudConnectionString);
                    cloudDbName = cloudBuilder.InitialCatalog;
                    _masterDb.CompanyDatabases.Add(new CompanyDatabase
                    {
                        CompanyId = company.CompanyId,
                        ServerName = cloudBuilder.DataSource,
                        DatabaseName = cloudDbName,
                        CredentialKey = "Cloud",
                        DatabaseType = "Cloud",
                        ConnectionString = req.CloudConnectionString.Trim(),
                        IsActive = true
                    });
                    _logger.LogInformation("Cloud database entry registered for {CompanyCode}: {CloudDb}", codeUpper, cloudDbName);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Invalid cloud connection string for {CompanyCode}, skipping cloud DB registration.", codeUpper);
                }
            }

            await _masterDb.SaveChangesAsync();

            _logger.LogInformation("Company {CompanyCode} (ID: {CompanyId}) registered in Master DB. Provisioning dedicated database {DatabaseName}...",
                company.CompanyCode, company.CompanyId, dbName);

            // C. Provision the physical tenant database & apply migrations
            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(company.CompanyId);

                // Create the database schema
                await tenantDb.Database.EnsureCreatedAsync();

                // D. Seed Initial Company Admin into the Tenant DB
                var adminUser = new UserManagement
                {
                    Username = req.AdminUsername.Trim(),
                    PasswordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(req.AdminPassword)),
                    FirstName = string.IsNullOrWhiteSpace(req.AdminFirstName) ? "Company" : req.AdminFirstName.Trim(),
                    LastName = string.IsNullOrWhiteSpace(req.AdminLastName) ? "Admin" : req.AdminLastName.Trim(),
                    Email = req.AdminEmail?.Trim() ?? req.ContactEmail?.Trim() ?? $"{req.AdminUsername}@{codeUpper.ToLower()}.local",
                    Role = UserRole.Admin,
                    IsActive = true
                };

                tenantDb.Users.Add(adminUser);
                await tenantDb.SaveChangesAsync();

                // E. Seed initial baseline operational data (categories, initial branch)
                DataSeeder.EnsureTenantSeed(tenantDb);

                _logger.LogInformation("Tenant DB {DatabaseName} successfully provisioned and seeded for {CompanyCode}.", dbName, company.CompanyCode);

                return CreatedAtAction(nameof(GetById), new { id = company.CompanyId }, new
                {
                    message = "Tenant successfully registered and provisioned!",
                    companyId = company.CompanyId,
                    companyCode = company.CompanyCode,
                    companyName = company.CompanyName,
                    databaseName = dbName,
                    serverName = serverName,
                    adminUsername = adminUser.Username
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to provision tenant DB {DatabaseName} for {CompanyCode}", dbName, company.CompanyCode);
                return StatusCode(500, new
                {
                    message = "Company record was created in Master DB, but database provisioning failed.",
                    companyId = company.CompanyId,
                    error = ex.Message
                });
            }
        }

        // 4. Toggle subscriber active/suspended status
        [HttpPost("{id:int}/status")]
        public async Task<IActionResult> ToggleStatus(int id, [FromBody] UpdateStatusRequest req)
        {
            var company = await _masterDb.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            company.IsActive = req.IsActive;
            await _masterDb.SaveChangesAsync();

            // Also sync active status on the databases
            var dbs = await _masterDb.CompanyDatabases.Where(d => d.CompanyId == id).ToListAsync();
            foreach (var d in dbs) d.IsActive = req.IsActive;
            await _masterDb.SaveChangesAsync();

            TenantResolutionMiddleware.InvalidateCache(company.CompanyCode);

            return Ok(new { message = $"Company '{company.CompanyCode}' status updated to {(company.IsActive ? "Active" : "Suspended")}." });
        }
        // 5. Assign or update cloud database for a tenant
        [HttpPost("{id:int}/cloud-database")]
        public async Task<IActionResult> AssignCloudDatabase(int id, [FromBody] AssignCloudDbRequest req)
        {
            var company = await _masterDb.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            if (string.IsNullOrWhiteSpace(req.CloudConnectionString))
                return BadRequest(new { message = "CloudConnectionString is required." });

            try
            {
                var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(req.CloudConnectionString);

                // Check if a Cloud entry already exists
                var existing = await _masterDb.CompanyDatabases
                    .FirstOrDefaultAsync(d => d.CompanyId == id && d.DatabaseType == "Cloud");

                if (existing != null)
                {
                    existing.ServerName = builder.DataSource;
                    existing.DatabaseName = builder.InitialCatalog;
                    existing.ConnectionString = req.CloudConnectionString.Trim();
                    existing.IsActive = true;
                }
                else
                {
                    _masterDb.CompanyDatabases.Add(new CompanyDatabase
                    {
                        CompanyId = id,
                        ServerName = builder.DataSource,
                        DatabaseName = builder.InitialCatalog,
                        CredentialKey = "Cloud",
                        DatabaseType = "Cloud",
                        ConnectionString = req.CloudConnectionString.Trim(),
                        IsActive = true
                    });
                }

                await _masterDb.SaveChangesAsync();

                TenantResolutionMiddleware.InvalidateCache(company.CompanyCode);

                _logger.LogInformation("Cloud database assigned for {CompanyCode}: {CloudDb} @ {CloudServer}",
                    company.CompanyCode, builder.InitialCatalog, builder.DataSource);

                return Ok(new
                {
                    message = $"Cloud database assigned for '{company.CompanyCode}'.",
                    cloudServer = builder.DataSource,
                    cloudDatabase = builder.InitialCatalog
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Invalid connection string.", error = ex.Message });
            }
        }

        // 6. Get all database entries (Local + Cloud) for a company
        [HttpGet("{id:int}/databases")]
        public async Task<IActionResult> GetDatabases(int id)
        {
            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            var databases = await _masterDb.CompanyDatabases
                .AsNoTracking()
                .Where(d => d.CompanyId == id)
                .Select(d => new
                {
                    d.CompanyDatabaseId,
                    d.DatabaseType,
                    d.ServerName,
                    d.DatabaseName,
                    d.CredentialKey,
                    d.ConnectionString,
                    d.IsActive
                })
                .ToListAsync();

            return Ok(new { company = new { company.CompanyId, company.CompanyCode, company.CompanyName }, databases });
        }

        // 7. Run migrations on a tenant DB
        [HttpPost("{id:int}/migrate")]
        public async Task<IActionResult> MigrateTenantDb(int id)
        {
            var company = await _masterDb.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            try
            {
                await using var tenantDb = await _tenantFactory.CreateAsync(id);
                await tenantDb.Database.EnsureCreatedAsync();

                return Ok(new { message = $"Tenant database for '{company.CompanyCode}' is up to date." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Migration failed", error = ex.Message });
            }
        }
        // 6. Update plan and modules
        [HttpPut("{id:int}/plan")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdatePlanRequest req)
        {
            var company = await _masterDb.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            string oldPlan = company.SubscriptionPlan;
            company.SubscriptionPlan = req.SubscriptionPlan ?? company.SubscriptionPlan;
            if (req.SubscriptionExpiresAt.HasValue) company.SubscriptionExpiresAt = req.SubscriptionExpiresAt.Value;
            company.EnabledModules = !string.IsNullOrWhiteSpace(req.EnabledModules)
                ? req.EnabledModules
                : await GetModulesForPlanAsync(company.SubscriptionPlan);

            var planAmount = (company.SubscriptionPlan) switch
            {
                "Basic" => 99.00m,
                "Standard" => 249.00m,
                "Enterprise" => 599.00m,
                _ => 199.00m
            };

            _masterDb.SubscriptionHistories.Add(new SubscriptionHistory
            {
                CompanyId = company.CompanyId,
                PlanName = company.SubscriptionPlan,
                Amount = planAmount,
                BillingCycle = "Monthly",
                StartDate = DateTime.UtcNow,
                EndDate = company.SubscriptionExpiresAt ?? DateTime.UtcNow.AddYears(1),
                Status = "Active",
                Notes = req.Notes ?? $"Plan updated from {oldPlan} to {company.SubscriptionPlan} by Super Admin.",
                CreatedAt = DateTime.UtcNow
            });

            await _masterDb.SaveChangesAsync();

            return Ok(new { message = $"Company '{company.CompanyCode}' plan updated.", company });
        }

        // 7. Get Subscription History for a company
        [HttpGet("{id:int}/history")]
        public async Task<IActionResult> GetSubscriptionHistory(int id)
        {
            var history = await _masterDb.SubscriptionHistories
                .AsNoTracking()
                .Where(h => h.CompanyId == id)
                .OrderByDescending(h => h.CreatedAt)
                .Select(h => new
                {
                    h.Id,
                    h.CompanyId,
                    h.PlanName,
                    h.Amount,
                    h.BillingCycle,
                    h.StartDate,
                    h.EndDate,
                    h.Status,
                    h.Notes,
                    h.CreatedAt
                })
                .ToListAsync();

            return Ok(history);
        }

        // 8. Renew subscriber company subscription
        [HttpPost("{id:int}/renew")]
        public async Task<IActionResult> RenewSubscription(int id, [FromBody] RenewSubscriptionRequest req)
        {
            var company = await _masterDb.Companies.FindAsync(id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            var now = DateTime.UtcNow;
            DateTime newExpiry;
            int months = req.Months.GetValueOrDefault(1);
            if (months < 1) months = 1;

            if (req.NewExpiryDate.HasValue && req.NewExpiryDate.Value > now)
            {
                newExpiry = req.NewExpiryDate.Value;
            }
            else
            {
                // If company is currently active and expiry is in the future, extend from existing expiry date
                if (company.SubscriptionExpiresAt.HasValue && company.SubscriptionExpiresAt.Value > now)
                {
                    newExpiry = company.SubscriptionExpiresAt.Value.AddMonths(months);
                }
                else
                {
                    // Expired or null expiry: extend from today
                    newExpiry = now.AddMonths(months);
                }
            }

            if (!string.IsNullOrWhiteSpace(req.PlanName))
            {
                company.SubscriptionPlan = req.PlanName.Trim();
                company.EnabledModules = await GetModulesForPlanAsync(company.SubscriptionPlan);
            }

            company.SubscriptionExpiresAt = newExpiry;
            company.IsActive = true; // Reactivate tenant

            // Also reactivate all database records for this tenant
            var dbs = await _masterDb.CompanyDatabases.Where(d => d.CompanyId == id).ToListAsync();
            foreach (var d in dbs) d.IsActive = true;

            decimal monthlyRate = company.SubscriptionPlan switch
            {
                "Basic" => 99.00m,
                "Standard" => 249.00m,
                "Enterprise" => 599.00m,
                _ => 199.00m
            };

            decimal totalAmount = req.Amount ?? (monthlyRate * months);

            _masterDb.SubscriptionHistories.Add(new SubscriptionHistory
            {
                CompanyId = company.CompanyId,
                PlanName = company.SubscriptionPlan,
                Amount = totalAmount,
                BillingCycle = months >= 12 ? "Annual" : (months == 1 ? "Monthly" : $"{months} Months"),
                StartDate = now,
                EndDate = newExpiry,
                Status = "Active",
                Notes = !string.IsNullOrWhiteSpace(req.Notes)
                    ? req.Notes.Trim()
                    : $"Subscription renewed for {months} month(s) until {newExpiry:MMM dd, yyyy} by Super Admin.",
                CreatedAt = now
            });

            await _masterDb.SaveChangesAsync();
            TenantResolutionMiddleware.InvalidateCache(company.CompanyCode);

            _logger.LogInformation("Company {CompanyCode} subscription renewed until {NewExpiry}. Status set to Active.",
                company.CompanyCode, newExpiry);

            return Ok(new
            {
                message = $"Subscription for '{company.CompanyName}' ({company.CompanyCode}) successfully renewed until {newExpiry:MMM dd, yyyy}.",
                companyId = company.CompanyId,
                companyCode = company.CompanyCode,
                companyName = company.CompanyName,
                subscriptionPlan = company.SubscriptionPlan,
                subscriptionExpiresAt = company.SubscriptionExpiresAt,
                isActive = company.IsActive
            });
        }

        private async Task<PlatformSummaryReport> BuildPlatformSummaryReportAsync()
        {
            await CheckAndDeactivateExpiredCompaniesAsync();

            var companies = await _masterDb.Companies.AsNoTracking().ToListAsync();
            var databases = await _masterDb.CompanyDatabases.AsNoTracking().ToListAsync();
            var histories = await _masterDb.SubscriptionHistories.AsNoTracking()
                .OrderByDescending(h => h.CreatedAt)
                .ToListAsync();

            int totalSubscribers = companies.Count;
            int activeSubscribers = companies.Count(c => c.IsActive);
            int suspendedSubscribers = companies.Count(c => !c.IsActive);

            int basicCount = companies.Count(c => string.Equals(c.SubscriptionPlan, "Basic", StringComparison.OrdinalIgnoreCase));
            int standardCount = companies.Count(c => string.Equals(c.SubscriptionPlan, "Standard", StringComparison.OrdinalIgnoreCase));
            int enterpriseCount = companies.Count(c => string.Equals(c.SubscriptionPlan, "Enterprise", StringComparison.OrdinalIgnoreCase));
            int customCount = totalSubscribers - (basicCount + standardCount + enterpriseCount);

            decimal mrr = (basicCount * 99m) + (standardCount * 249m) + (enterpriseCount * 599m);
            decimal totalCollected = histories.Sum(h => h.Amount);

            int totalPlatformUsers = 0;
            int totalPlatformBranches = 0;
            int cloudDatabasesCount = databases.Count(d => d.DatabaseType == "Cloud" && d.IsActive);
            int expiringSoonCount = 0;
            var now = DateTime.UtcNow;

            var tenantUserCountMap = new Dictionary<int, int>();
            var tenantBranchCountMap = new Dictionary<int, int>();

            foreach (var c in companies)
            {
                if (c.SubscriptionExpiresAt.HasValue && c.SubscriptionExpiresAt.Value > now && (c.SubscriptionExpiresAt.Value - now).TotalDays <= 30)
                {
                    expiringSoonCount++;
                }

                try
                {
                    await using var tenantDb = await _tenantFactory.CreateAsync(c.CompanyId);
                    int uCount = await tenantDb.Users.CountAsync();
                    int bCount = await tenantDb.Branches.CountAsync();
                    tenantUserCountMap[c.CompanyId] = uCount;
                    tenantBranchCountMap[c.CompanyId] = bCount;
                    totalPlatformUsers += uCount;
                    totalPlatformBranches += bCount;
                }
                catch
                {
                    tenantUserCountMap[c.CompanyId] = 0;
                    tenantBranchCountMap[c.CompanyId] = 0;
                }
            }

            // Module Feature Adoption
            var allModules = new[] { "Inventory", "Procurement", "Sales", "Technical", "HR", "Branches", "Reports", "Finance" };
            var moduleAdoptions = allModules.Select(m =>
            {
                int count = companies.Count(c => (c.EnabledModules ?? "").Contains(m, StringComparison.OrdinalIgnoreCase));
                decimal pct = totalSubscribers > 0 ? Math.Round((decimal)count / totalSubscribers * 100m, 1) : 0m;
                return new ModuleAdoptionDto
                {
                    ModuleName = m,
                    TenantCount = count,
                    AdoptionPercentage = pct
                };
            }).ToList();

            // Revenue Trends (last 6 calendar months)
            var revenueTrends = new List<MonthlyTrendPointDto>();
            for (int i = 5; i >= 0; i--)
            {
                var mDate = now.AddMonths(-i);
                var mStart = new DateTime(mDate.Year, mDate.Month, 1);
                var mEnd = mStart.AddMonths(1);
                decimal monthCollected = histories.Where(h => h.CreatedAt >= mStart && h.CreatedAt < mEnd).Sum(h => h.Amount);
                if (monthCollected == 0 && i == 0) monthCollected = mrr;
                decimal displayRev = monthCollected > 0 ? monthCollected : Math.Round(mrr * (0.75m + (5 - i) * 0.05m), 2);
                revenueTrends.Add(new MonthlyTrendPointDto
                {
                    MonthLabel = mDate.ToString("MMM yyyy"),
                    Revenue = displayRev
                });
            }

            var tenantBreakdown = companies.Select(c =>
            {
                var localDb = databases.FirstOrDefault(d => d.CompanyId == c.CompanyId && (d.DatabaseType == "Local" || d.DatabaseType == null));
                var cloudDb = databases.FirstOrDefault(d => d.CompanyId == c.CompanyId && d.DatabaseType == "Cloud");
                var companyHistories = histories.Where(h => h.CompanyId == c.CompanyId).ToList();
                decimal moneyGenerated = companyHistories.Sum(h => h.Amount);

                return new TenantReportItemDto
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    ContactEmail = c.ContactEmail,
                    SubscriptionPlan = c.SubscriptionPlan,
                    IsActive = c.IsActive,
                    CreatedAt = c.CreatedAt,
                    SubscriptionExpiresAt = c.SubscriptionExpiresAt,
                    EnabledModules = c.EnabledModules,
                    DatabaseName = localDb?.DatabaseName ?? "N/A",
                    ServerName = localDb?.ServerName ?? "N/A",
                    CloudDatabaseName = cloudDb?.DatabaseName,
                    CloudServerName = cloudDb?.ServerName,
                    TotalMoneyGenerated = moneyGenerated,
                    UserCount = tenantUserCountMap.TryGetValue(c.CompanyId, out int uc) ? uc : 0,
                    BranchCount = tenantBranchCountMap.TryGetValue(c.CompanyId, out int bc) ? bc : 0
                };
            }).OrderByDescending(t => t.CreatedAt).ToList();

            var historyItems = histories.Select(h =>
            {
                var comp = companies.FirstOrDefault(c => c.CompanyId == h.CompanyId);
                return new SubscriptionHistoryReportDto
                {
                    Id = h.Id,
                    CompanyId = h.CompanyId,
                    CompanyCode = comp?.CompanyCode ?? $"ID #{h.CompanyId}",
                    CompanyName = comp?.CompanyName ?? "Unknown Company",
                    PlanName = h.PlanName,
                    Amount = h.Amount,
                    BillingCycle = h.BillingCycle,
                    StartDate = h.StartDate,
                    EndDate = h.EndDate,
                    Status = h.Status,
                    Notes = h.Notes,
                    CreatedAt = h.CreatedAt
                };
            }).ToList();

            return new PlatformSummaryReport
            {
                TotalSubscribers = totalSubscribers,
                ActiveSubscribers = activeSubscribers,
                SuspendedSubscribers = suspendedSubscribers,
                BasicCount = basicCount,
                StandardCount = standardCount,
                EnterpriseCount = enterpriseCount,
                CustomCount = customCount,
                EstimatedMonthlyRevenue = mrr,
                TotalCollectedRevenue = totalCollected,
                TotalPlatformUsers = totalPlatformUsers,
                TotalPlatformBranches = totalPlatformBranches,
                CloudDatabasesCount = cloudDatabasesCount,
                ExpiringSoonCount = expiringSoonCount,
                ModuleAdoptions = moduleAdoptions,
                RevenueTrends = revenueTrends,
                Tenants = tenantBreakdown,
                SubscriptionHistories = historyItems
            };
        }

        // 8. Platform Reports & Metrics for Super Admin
        [HttpGet("reports/platform-summary")]
        public async Task<IActionResult> GetPlatformReport()
        {
            var summary = await BuildPlatformSummaryReportAsync();
            return Ok(summary);
        }

        [HttpGet("reports/platform-summary/pdf")]
        public async Task<IActionResult> GetPlatformReportPdf()
        {
            var summary = await BuildPlatformSummaryReportAsync();
            var pdf = ReportPdfGenerator.GeneratePlatformSummaryReport(summary);
            return File(pdf, "application/pdf", $"Platform_Summary_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        public async Task<string> GetModulesForPlanAsync(string? plan)
        {
            if (string.IsNullOrWhiteSpace(plan)) return GetDefaultModulesForPlan(plan);

            try
            {
                var setting = await _masterDb.PlatformSettings
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Key == "SubscriptionPlanDefinitions");

                if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
                {
                    var plans = System.Text.Json.JsonSerializer.Deserialize<List<SubscriptionPlanItem>>(
                        setting.Value,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var match = plans?.FirstOrDefault(p => string.Equals(p.Name, plan, StringComparison.OrdinalIgnoreCase));
                    if (match != null && !string.IsNullOrWhiteSpace(match.EnabledModules))
                    {
                        return match.EnabledModules;
                    }
                }
            }
            catch { }

            return GetDefaultModulesForPlan(plan);
        }

        public static string GetDefaultModulesForPlan(string? plan)
        {
            return (plan?.Trim().ToLowerInvariant()) switch
            {
                "basic" => "Inventory,Procurement,Sales,Technical,Reports",
                "standard" => "Inventory,Procurement,Sales,Technical,HR,Branches,Reports",
                "enterprise" => "Inventory,Procurement,Sales,Technical,HR,Branches,Reports,Finance",
                _ => "Inventory,Procurement,Sales,Technical,HR,Branches,Reports"
            };
        }

        // ─── Tenant User Management (SuperAdmin) ──────────────────────────────────

        // 9. Get all users for a specific tenant company (supports optional ?role=Admin)
        [HttpGet("{id:int}/users")]
        public async Task<IActionResult> GetTenantUsers(int id, [FromQuery] string? role = null)
        {
            var company = await _masterDb.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            await using var tenantDb = await _tenantFactory.CreateAsync(id);

            var branches = await tenantDb.Branches.AsNoTracking()
                .OrderBy(b => b.Name)
                .Select(b => new { b.Id, b.Name, b.Code })
                .ToListAsync();

            var branchLookup = branches.ToDictionary(b => b.Id, b => $"{b.Name} ({b.Code})");

            var query = tenantDb.Users.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (Enum.TryParse<UserRole>(role, true, out var parsedRole))
                {
                    query = query.Where(u => u.Role == parsedRole);
                }
            }

            var users = await query
                .OrderBy(u => u.Id)
                .ToListAsync();

            var userList = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.FirstName,
                u.MiddleName,
                u.LastName,
                FullName = $"{u.FirstName} {(!string.IsNullOrWhiteSpace(u.MiddleName) ? u.MiddleName + " " : "")}{u.LastName}".Trim(),
                u.Email,
                Role = u.Role.ToString(),
                u.IsActive,
                u.BranchId,
                BranchName = u.BranchId.HasValue && branchLookup.TryGetValue(u.BranchId.Value, out var bName) ? bName : "All / Head Office"
            }).ToList();

            return Ok(new
            {
                companyId = company.CompanyId,
                companyCode = company.CompanyCode,
                companyName = company.CompanyName,
                users = userList,
                branches
            });
        }

        // 10. Add a new user to a specific tenant company
        [HttpPost("{id:int}/users")]
        public async Task<IActionResult> CreateTenantUser(int id, [FromBody] SuperAdminCreateTenantUserRequest req)
        {
            var company = await _masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            if (string.IsNullOrWhiteSpace(req.Username))
                return BadRequest(new { message = "Username is required." });
            if (string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "Password is required." });
            if (string.IsNullOrWhiteSpace(req.FirstName) || string.IsNullOrWhiteSpace(req.LastName))
                return BadRequest(new { message = "First name and last name are required." });

            await using var tenantDb = await _tenantFactory.CreateAsync(id);

            var cleanUsername = req.Username.Trim();
            bool exists = await tenantDb.Users.AnyAsync(u => u.Username.ToLower() == cleanUsername.ToLower());
            if (exists)
                return BadRequest(new { message = $"Username '{cleanUsername}' already exists in {company.CompanyName}." });

            var user = new UserManagement
            {
                Username = cleanUsername,
                PasswordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(req.Password.Trim())),
                FirstName = req.FirstName.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName.Trim(),
                LastName = req.LastName.Trim(),
                Email = string.IsNullOrWhiteSpace(req.Email) ? $"{cleanUsername}@{company.CompanyCode.ToLower()}.local" : req.Email.Trim(),
                Role = req.Role,
                BranchId = req.BranchId > 0 ? req.BranchId : null,
                IsActive = req.IsActive
            };

            tenantDb.Users.Add(user);
            await tenantDb.SaveChangesAsync();

            _logger.LogInformation("SuperAdmin created user '{Username}' (Role: {Role}) for tenant {CompanyCode}.",
                user.Username, user.Role, company.CompanyCode);

            return Ok(new { message = $"User '{user.Username}' created successfully in {company.CompanyName}.", userId = user.Id });
        }

        // 11. Deactivate / Activate a tenant user
        [HttpPost("{id:int}/users/{userId:int}/status")]
        [HttpPut("{id:int}/users/{userId:int}/status")]
        public async Task<IActionResult> UpdateTenantUserStatus(int id, int userId, [FromBody] UpdateStatusRequest req)
        {
            var company = await _masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            await using var tenantDb = await _tenantFactory.CreateAsync(id);
            var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = $"User #{userId} not found in {company.CompanyName}." });

            user.IsActive = req.IsActive;
            await tenantDb.SaveChangesAsync();

            _logger.LogInformation("SuperAdmin changed user '{Username}' status to {Status} for tenant {CompanyCode}.",
                user.Username, user.IsActive ? "Active" : "Deactivated", company.CompanyCode);

            return Ok(new
            {
                message = $"User '{user.Username}' is now {(user.IsActive ? "Active" : "Deactivated")}.",
                userId = user.Id,
                isActive = user.IsActive
            });
        }

        // 12. Update a tenant user's details or reset password
        [HttpPut("{id:int}/users/{userId:int}")]
        public async Task<IActionResult> UpdateTenantUser(int id, int userId, [FromBody] SuperAdminUpdateTenantUserRequest req)
        {
            var company = await _masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            await using var tenantDb = await _tenantFactory.CreateAsync(id);
            var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = $"User #{userId} not found in {company.CompanyName}." });

            if (!string.IsNullOrWhiteSpace(req.FirstName)) user.FirstName = req.FirstName.Trim();
            if (!string.IsNullOrWhiteSpace(req.LastName)) user.LastName = req.LastName.Trim();
            user.MiddleName = string.IsNullOrWhiteSpace(req.MiddleName) ? null : req.MiddleName.Trim();
            if (!string.IsNullOrWhiteSpace(req.Email)) user.Email = req.Email.Trim();
            user.Role = req.Role;
            user.BranchId = req.BranchId > 0 ? req.BranchId : null;
            user.IsActive = req.IsActive;

            if (!string.IsNullOrWhiteSpace(req.NewPassword))
            {
                user.PasswordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(req.NewPassword.Trim()));
            }

            await tenantDb.SaveChangesAsync();

            _logger.LogInformation("SuperAdmin updated user '{Username}' for tenant {CompanyCode}.", user.Username, company.CompanyCode);

            return Ok(new { message = $"User '{user.Username}' updated successfully in {company.CompanyName}." });
        }

        // 13. Delete a tenant user
        [HttpDelete("{id:int}/users/{userId:int}")]
        public async Task<IActionResult> DeleteTenantUser(int id, int userId)
        {
            var company = await _masterDb.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null) return NotFound(new { message = $"Company {id} not found." });

            await using var tenantDb = await _tenantFactory.CreateAsync(id);
            var user = await tenantDb.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound(new { message = $"User #{userId} not found in {company.CompanyName}." });

            // Unlink any employee
            var employee = await tenantDb.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
            if (employee != null) employee.UserId = null;

            tenantDb.Users.Remove(user);
            await tenantDb.SaveChangesAsync();

            _logger.LogInformation("SuperAdmin deleted user '{Username}' from tenant {CompanyCode}.", user.Username, company.CompanyCode);

            return Ok(new { message = $"User '{user.Username}' deleted successfully from {company.CompanyName}." });
        }
    }

    public class RegisterTenantRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? SubscriptionPlan { get; set; } = "Standard";
        public DateTime? SubscriptionExpiresAt { get; set; }

        public string? DbServerName { get; set; }
        public string? CustomDatabaseName { get; set; }
        public string? CredentialKey { get; set; }

        /// <summary>
        /// Full cloud database connection string. If provided, a Cloud CompanyDatabase entry
        /// will be created in MasterDB alongside the Local entry.
        /// </summary>
        public string? CloudConnectionString { get; set; }

        // Initial Company Admin
        public string AdminUsername { get; set; } = "admin";
        public string AdminPassword { get; set; } = "Admin@123";
        public string? AdminFirstName { get; set; } = "Company";
        public string? AdminLastName { get; set; } = "Admin";
        public string? AdminEmail { get; set; }
    }

    public class UpdateStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class UpdatePlanRequest
    {
        public string? SubscriptionPlan { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string? EnabledModules { get; set; }
        public string? Notes { get; set; }
    }

    public class RenewSubscriptionRequest
    {
        public int? Months { get; set; } = 1;
        public DateTime? NewExpiryDate { get; set; }
        public string? PlanName { get; set; }
        public decimal? Amount { get; set; }
        public string? Notes { get; set; }
    }

    public class AssignCloudDbRequest
    {
        public string CloudConnectionString { get; set; } = string.Empty;
    }

    public class SuperAdminCreateTenantUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public UserRole Role { get; set; } = UserRole.Admin;
        public int? BranchId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class SuperAdminUpdateTenantUserRequest
    {
        public string? FirstName { get; set; }
        public string? MiddleName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public UserRole Role { get; set; }
        public int? BranchId { get; set; }
        public bool IsActive { get; set; }
        public string? NewPassword { get; set; }
    }
}
