using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure.services
{
    public class CloudSyncService : ICloudSyncService
    {
        private readonly MasterErpDbContext _localMasterDb;
        private readonly ITenantDbContextFactory _tenantFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CloudSyncService> _logger;

        private static readonly object _syncLock = new();
        private static readonly CloudSyncStatus _currentStatus = new()
        {
            IsOnline = false,
            IsSyncing = false,
            State = SyncState.PendingSync,
            Message = "Sync engine initialized."
        };

        public CloudSyncService(
            MasterErpDbContext localMasterDb,
            ITenantDbContextFactory tenantFactory,
            IConfiguration config,
            ILogger<CloudSyncService> logger)
        {
            _localMasterDb = localMasterDb;
            _tenantFactory = tenantFactory;
            _config = config;
            _logger = logger;

            var masterConn = GetMasterCloudConnectionString();
            if (!string.IsNullOrWhiteSpace(masterConn))
            {
                var builder = new SqlConnectionStringBuilder(masterConn);
                _currentStatus.CloudMasterHost = builder.DataSource;
            }
        }

        private string? GetMasterCloudConnectionString()
        {
            return _config["CloudSync:MasterCloudConnection"]
                ?? _config.GetConnectionString("CloudMasterErp");
        }

        private string? GetTenantCloudConnectionString(string companyCode)
        {
            // This synchronous method is used for quick lookups.
            // The actual MasterDB-based resolution happens in GetTenantCloudConnectionStringAsync.
            return _config[$"CloudSync:TenantCloudConnections:{companyCode}"]
                ?? _config.GetConnectionString($"CloudTenant_{companyCode}");
        }

        /// <summary>
        /// Resolve cloud connection string from MasterDB first, then fall back to appsettings.json.
        /// </summary>
        private async Task<string?> GetTenantCloudConnectionStringAsync(string companyCode)
        {
            // 1. Resolve from MasterDB via the factory (which checks CompanyDatabases with DatabaseType='Cloud')
            var connStr = await _tenantFactory.GetCloudConnectionStringByCodeAsync(companyCode);
            if (!string.IsNullOrWhiteSpace(connStr))
                return connStr;

            // 2. Fallback to appsettings.json config
            return _config[$"CloudSync:TenantCloudConnections:{companyCode}"]
                ?? _config.GetConnectionString($"CloudTenant_{companyCode}");
        }

        public async Task<CloudSyncStatus> GetSyncStatusAsync()
        {
            if (_currentStatus.IsSyncing)
            {
                return _currentStatus;
            }

            var masterConn = GetMasterCloudConnectionString();
            if (string.IsNullOrWhiteSpace(masterConn))
            {
                _currentStatus.IsOnline = false;
                _currentStatus.State = SyncState.Offline;
                _currentStatus.Message = "Cloud connection string not configured in appsettings.json.";
                return _currentStatus;
            }

            bool online = await TestConnectionAsync(masterConn);
            _currentStatus.IsOnline = online;

            if (!online)
            {
                _currentStatus.State = SyncState.Offline;
                _currentStatus.Message = "Cannot reach cloud database server. Operating in local mode.";
            }
            else if (_currentStatus.LastSyncUtc.HasValue)
            {
                _currentStatus.State = SyncState.Synced;
                _currentStatus.Message = $"Cloud synced. Last sync at {_currentStatus.LastSyncUtc.Value.ToLocalTime():h:mm tt}.";
            }
            else
            {
                _currentStatus.State = SyncState.PendingSync;
                _currentStatus.Message = "Cloud connected. Ready to sync local changes.";
            }

            return _currentStatus;
        }

        public async Task<CloudSyncResult> SyncAllAsync()
        {
            lock (_syncLock)
            {
                if (_currentStatus.IsSyncing)
                {
                    return new CloudSyncResult
                    {
                        Success = false,
                        Message = "A sync operation is already in progress."
                    };
                }
                _currentStatus.IsSyncing = true;
                _currentStatus.State = SyncState.Syncing;
                _currentStatus.Message = "Syncing local database with cloud database...";
            }

            var result = new CloudSyncResult();

            try
            {
                var masterConn = GetMasterCloudConnectionString();
                if (string.IsNullOrWhiteSpace(masterConn))
                {
                    throw new InvalidOperationException("CloudSync:MasterCloudConnection is not configured in appsettings.json.");
                }

                // 1. Check connectivity
                bool online = await TestConnectionAsync(masterConn);
                if (!online)
                {
                    _currentStatus.IsOnline = false;
                    _currentStatus.State = SyncState.Offline;
                    _currentStatus.Message = "Failed to connect to cloud database server. Working in local offline mode.";
                    result.Success = false;
                    result.Message = _currentStatus.Message;
                    return result;
                }

                _currentStatus.IsOnline = true;
                int totalEntitiesSynced = 0;

                // 2. Sync Master Database (Companies, CompanyDatabases, SubscriptionHistories, SuperAdmins)
                totalEntitiesSynced += await SyncMasterDatabaseAsync(masterConn, result);

                // 3. Sync each active tenant that has a cloud configuration
                var localCompanies = await _localMasterDb.Companies.AsNoTracking().ToListAsync();
                foreach (var company in localCompanies)
                {
                    var tenantConn = await GetTenantCloudConnectionStringAsync(company.CompanyCode);
                    if (!string.IsNullOrWhiteSpace(tenantConn))
                    {
                        try
                        {
                            bool tenantDbOnline = await TestConnectionAsync(tenantConn);
                            if (tenantDbOnline)
                            {
                                int tenantCount = await SyncSingleTenantDatabaseAsync(company.CompanyCode, tenantConn, result);
                                totalEntitiesSynced += tenantCount;
                                _currentStatus.TenantStatuses[company.CompanyCode] = $"Synced ({tenantCount} items)";
                            }
                            else
                            {
                                _currentStatus.TenantStatuses[company.CompanyCode] = "Cloud DB unreachable";
                                result.Details.Add($"Tenant {company.CompanyCode}: Cloud database unreachable.");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed syncing tenant {CompanyCode} to cloud", company.CompanyCode);
                            _currentStatus.TenantStatuses[company.CompanyCode] = $"Sync Error: {ex.Message}";
                            result.Details.Add($"Tenant {company.CompanyCode}: Error: {ex.Message}");
                        }
                    }
                    else
                    {
                        _currentStatus.TenantStatuses[company.CompanyCode] = "No cloud database mapped";
                    }
                }

                _currentStatus.LastSyncUtc = DateTime.UtcNow;
                _currentStatus.SyncedEntitiesCount = totalEntitiesSynced;
                _currentStatus.State = SyncState.Synced;
                _currentStatus.Message = $"All databases synced successfully ({totalEntitiesSynced} records pushed).";

                result.Success = true;
                result.RecordsPushed = totalEntitiesSynced;
                result.Message = _currentStatus.Message;
                result.SyncTimestamp = DateTime.UtcNow;

                _logger.LogInformation("Cloud sync completed successfully. Total records pushed: {Count}", totalEntitiesSynced);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cloud sync failed");
                _currentStatus.State = SyncState.Error;
                _currentStatus.Message = "Sync failed: " + ex.Message;

                result.Success = false;
                result.Message = _currentStatus.Message;
                result.Details.Add(ex.ToString());
                return result;
            }
            finally
            {
                _currentStatus.IsSyncing = false;
            }
        }

        public async Task<CloudSyncResult> SyncTenantAsync(string companyCode)
        {
            var tenantConn = await GetTenantCloudConnectionStringAsync(companyCode);
            if (string.IsNullOrWhiteSpace(tenantConn))
            {
                return new CloudSyncResult
                {
                    Success = false,
                    Message = $"No cloud connection configured for tenant {companyCode}."
                };
            }

            var result = new CloudSyncResult();
            try
            {
                int count = await SyncSingleTenantDatabaseAsync(companyCode, tenantConn, result);
                result.Success = true;
                result.RecordsPushed = count;
                result.Message = $"Tenant {companyCode} synced successfully ({count} records).";
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Tenant {companyCode} sync failed: " + ex.Message;
                return result;
            }
        }

        private async Task<int> SyncMasterDatabaseAsync(string cloudConn, CloudSyncResult result)
        {
            int count = 0;
            var cloudOptions = new DbContextOptionsBuilder<MasterErpDbContext>()
                .UseSqlServer(cloudConn, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options;

            await using var cloudMaster = new MasterErpDbContext(cloudOptions);
            await EnsureMasterSchemaAsync(cloudMaster);

            // A. Sync Companies
            var localCompanies = await _localMasterDb.Companies.AsNoTracking().ToListAsync();
            var cloudCompanies = await cloudMaster.Companies.ToListAsync();

            foreach (var localC in localCompanies)
            {
                var match = cloudCompanies.FirstOrDefault(c => string.Equals(c.CompanyCode, localC.CompanyCode, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudMaster.Companies.Add(new Company
                    {
                        CompanyCode = localC.CompanyCode,
                        CompanyName = localC.CompanyName,
                        ContactEmail = localC.ContactEmail,
                        ContactPhone = localC.ContactPhone,
                        SubscriptionPlan = localC.SubscriptionPlan,
                        SubscriptionExpiresAt = localC.SubscriptionExpiresAt,
                        EnabledModules = localC.EnabledModules,
                        IsActive = localC.IsActive,
                        CreatedAt = localC.CreatedAt
                    });
                    count++;
                }
                else
                {
                    match.CompanyName = localC.CompanyName;
                    match.ContactEmail = localC.ContactEmail;
                    match.ContactPhone = localC.ContactPhone;
                    match.SubscriptionPlan = localC.SubscriptionPlan;
                    match.SubscriptionExpiresAt = localC.SubscriptionExpiresAt;
                    match.EnabledModules = localC.EnabledModules;
                    match.IsActive = localC.IsActive;
                }
            }
            await cloudMaster.SaveChangesAsync();

            // Refresh cloud companies with their generated IDs
            var refreshedCloudCompanies = await cloudMaster.Companies.ToListAsync();

            // B. Sync SuperAdmin users
            var localAdmins = await _localMasterDb.SuperAdmins.AsNoTracking().ToListAsync();
            var cloudAdmins = await cloudMaster.SuperAdmins.ToListAsync();

            foreach (var la in localAdmins)
            {
                var match = cloudAdmins.FirstOrDefault(a => string.Equals(a.Username, la.Username, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudMaster.SuperAdmins.Add(new SuperAdminUser
                    {
                        Username = la.Username,
                        PasswordHash = la.PasswordHash,
                        Email = la.Email,
                        FullName = la.FullName,
                        IsActive = la.IsActive,
                        CreatedAt = la.CreatedAt
                    });
                    count++;
                }
                else
                {
                    match.PasswordHash = la.PasswordHash;
                    match.Email = la.Email;
                    match.FullName = la.FullName;
                    match.IsActive = la.IsActive;
                }
            }

            // C. Sync SubscriptionHistories
            var localHistories = await _localMasterDb.SubscriptionHistories.AsNoTracking().ToListAsync();
            var cloudHistories = await cloudMaster.SubscriptionHistories.ToListAsync();

            foreach (var lh in localHistories)
            {
                var localComp = localCompanies.FirstOrDefault(c => c.CompanyId == lh.CompanyId);
                if (localComp == null) continue;
                var cloudComp = refreshedCloudCompanies.FirstOrDefault(c => string.Equals(c.CompanyCode, localComp.CompanyCode, StringComparison.OrdinalIgnoreCase));
                if (cloudComp == null) continue;

                var match = cloudHistories.FirstOrDefault(h => h.CompanyId == cloudComp.CompanyId &&
                                                               string.Equals(h.PlanName, lh.PlanName, StringComparison.OrdinalIgnoreCase) &&
                                                               Math.Abs((h.StartDate - lh.StartDate).TotalMinutes) < 5);
                if (match == null)
                {
                    cloudMaster.SubscriptionHistories.Add(new SubscriptionHistory
                    {
                        CompanyId = cloudComp.CompanyId,
                        PlanName = lh.PlanName,
                        Amount = lh.Amount,
                        BillingCycle = lh.BillingCycle,
                        StartDate = lh.StartDate,
                        EndDate = lh.EndDate,
                        Status = lh.Status,
                        Notes = lh.Notes,
                        CreatedAt = lh.CreatedAt
                    });
                    count++;
                }
            }

            // D. Sync CompanyDatabases (both Local and Cloud entries)
            var localDbs = await _localMasterDb.CompanyDatabases.AsNoTracking().ToListAsync();
            var cloudDbs = await cloudMaster.CompanyDatabases.ToListAsync();

            foreach (var ldb in localDbs)
            {
                var localComp = localCompanies.FirstOrDefault(c => c.CompanyId == ldb.CompanyId);
                if (localComp == null) continue;
                var cloudComp = refreshedCloudCompanies.FirstOrDefault(c => string.Equals(c.CompanyCode, localComp.CompanyCode, StringComparison.OrdinalIgnoreCase));
                if (cloudComp == null) continue;

                // Match by CompanyId + DatabaseName + DatabaseType to properly distinguish Local vs Cloud entries
                var dbType = ldb.DatabaseType ?? "Local";
                var match = cloudDbs.FirstOrDefault(d => d.CompanyId == cloudComp.CompanyId &&
                                                         string.Equals(d.DatabaseName, ldb.DatabaseName, StringComparison.OrdinalIgnoreCase) &&
                                                         string.Equals(d.DatabaseType ?? "Local", dbType, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudMaster.CompanyDatabases.Add(new CompanyDatabase
                    {
                        CompanyId = cloudComp.CompanyId,
                        ServerName = ldb.ServerName,
                        DatabaseName = ldb.DatabaseName,
                        CredentialKey = ldb.CredentialKey,
                        DatabaseType = dbType,
                        ConnectionString = ldb.ConnectionString,
                        IsActive = ldb.IsActive
                    });
                    count++;
                }
                else
                {
                    match.ServerName = ldb.ServerName;
                    match.CredentialKey = ldb.CredentialKey;
                    match.DatabaseType = dbType;
                    match.ConnectionString = ldb.ConnectionString;
                    match.IsActive = ldb.IsActive;
                }
            }

            await cloudMaster.SaveChangesAsync();
            result.Details.Add($"Master Database: Synced {count} records into cloud.");
            return count;
        }

        private async Task<int> SyncSingleTenantDatabaseAsync(string companyCode, string cloudConn, CloudSyncResult result)
        {
            int count = 0;
            await using var cloudTenant = _tenantFactory.CreateFromConnectionString(cloudConn);
            await EnsureTenantSchemaAsync(cloudTenant);

            await using var localTenant = await _tenantFactory.CreateByCodeAsync(companyCode);

            // 1. Sync Branches
            var localBranches = await localTenant.Branches.AsNoTracking().ToListAsync();
            var cloudBranches = await cloudTenant.Branches.ToListAsync();
            foreach (var lb in localBranches)
            {
                var match = cloudBranches.FirstOrDefault(b => string.Equals(b.Code, lb.Code, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.Branches.Add(new Branch
                    {
                        Name = lb.Name,
                        Code = lb.Code,
                        Address = lb.Address,
                        IsActive = lb.IsActive
                    });
                    count++;
                }
                else
                {
                    match.Name = lb.Name;
                    match.Address = lb.Address;
                    match.IsActive = lb.IsActive;
                }
            }
            await cloudTenant.SaveChangesAsync();
            var refreshedCloudBranches = await cloudTenant.Branches.ToListAsync();

            // 2. Sync DeviceCategories
            var localCats = await localTenant.DeviceCategories.AsNoTracking().ToListAsync();
            var cloudCats = await cloudTenant.DeviceCategories.ToListAsync();
            foreach (var lc in localCats)
            {
                var match = cloudCats.FirstOrDefault(c => string.Equals(c.Name, lc.Name, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.DeviceCategories.Add(new DeviceCategory
                    {
                        Name = lc.Name,
                        Description = lc.Description
                    });
                    count++;
                }
                else
                {
                    match.Description = lc.Description;
                }
            }
            await cloudTenant.SaveChangesAsync();
            var refreshedCloudCats = await cloudTenant.DeviceCategories.ToListAsync();

            // 3. Sync Users
            var localUsers = await localTenant.Users.AsNoTracking().ToListAsync();
            var cloudUsers = await cloudTenant.Users.ToListAsync();
            foreach (var lu in localUsers)
            {
                var localBranch = localBranches.FirstOrDefault(b => b.Id == lu.BranchId);
                var cloudBranchId = localBranch != null
                    ? refreshedCloudBranches.FirstOrDefault(b => string.Equals(b.Code, localBranch.Code, StringComparison.OrdinalIgnoreCase))?.Id
                    : (int?)null;

                var match = cloudUsers.FirstOrDefault(u => string.Equals(u.Username, lu.Username, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.Users.Add(new UserManagement
                    {
                        Username = lu.Username,
                        PasswordHash = lu.PasswordHash,
                        FirstName = lu.FirstName,
                        LastName = lu.LastName,
                        Email = lu.Email,
                        Role = lu.Role,
                        BranchId = cloudBranchId,
                        IsActive = lu.IsActive
                    });
                    count++;
                }
                else
                {
                    match.PasswordHash = lu.PasswordHash;
                    match.FirstName = lu.FirstName;
                    match.LastName = lu.LastName;
                    match.Email = lu.Email;
                    match.Role = lu.Role;
                    match.BranchId = cloudBranchId;
                    match.IsActive = lu.IsActive;
                }
            }
            await cloudTenant.SaveChangesAsync();
            var refreshedCloudUsers = await cloudTenant.Users.ToListAsync();

            // 4. Sync Employees
            var localEmps = await localTenant.Employees.AsNoTracking().ToListAsync();
            var cloudEmps = await cloudTenant.Employees.ToListAsync();
            foreach (var le in localEmps)
            {
                var localBranch = localBranches.FirstOrDefault(b => b.Id == le.BranchId);
                var cloudBranchId = localBranch != null
                    ? refreshedCloudBranches.FirstOrDefault(b => string.Equals(b.Code, localBranch.Code, StringComparison.OrdinalIgnoreCase))?.Id
                    : (int?)null;

                var localUser = localUsers.FirstOrDefault(u => u.Id == le.UserId);
                var cloudUserId = localUser != null
                    ? refreshedCloudUsers.FirstOrDefault(u => string.Equals(u.Username, localUser.Username, StringComparison.OrdinalIgnoreCase))?.Id
                    : (int?)null;

                var match = cloudEmps.FirstOrDefault(e => string.Equals(e.EmployeeCode, le.EmployeeCode, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.Employees.Add(new Employee
                    {
                        EmployeeCode = le.EmployeeCode,
                        FirstName = le.FirstName,
                        MiddleName = le.MiddleName,
                        LastName = le.LastName,
                        ContactNumber = le.ContactNumber,
                        EmailAddress = le.EmailAddress,
                        Street = le.Street,
                        Barangay = le.Barangay,
                        City = le.City,
                        Province = le.Province,
                        Country = le.Country,
                        Position = le.Position,
                        Department = le.Department,
                        DateHired = le.DateHired,
                        Status = le.Status,
                        PayType = le.PayType,
                        PayRate = le.PayRate,
                        BranchId = cloudBranchId,
                        UserId = cloudUserId
                    });
                    count++;
                }
                else
                {
                    match.FirstName = le.FirstName;
                    match.LastName = le.LastName;
                    match.Position = le.Position;
                    match.Department = le.Department;
                    match.PayRate = le.PayRate;
                    match.BranchId = cloudBranchId;
                    match.UserId = cloudUserId;
                }
            }
            await cloudTenant.SaveChangesAsync();

            // 5. Sync Inventories
            var localInvs = await localTenant.Inventories.AsNoTracking().ToListAsync();
            var cloudInvs = await cloudTenant.Inventories.ToListAsync();
            foreach (var li in localInvs)
            {
                var localBranch = localBranches.FirstOrDefault(b => b.Id == li.BranchId);
                var cloudBranch = localBranch != null
                    ? refreshedCloudBranches.FirstOrDefault(b => string.Equals(b.Code, localBranch.Code, StringComparison.OrdinalIgnoreCase))
                    : refreshedCloudBranches.FirstOrDefault();

                var localCat = localCats.FirstOrDefault(c => c.Id == li.DeviceCategoryId);
                var cloudCat = localCat != null
                    ? refreshedCloudCats.FirstOrDefault(c => string.Equals(c.Name, localCat.Name, StringComparison.OrdinalIgnoreCase))
                    : refreshedCloudCats.FirstOrDefault();

                if (cloudBranch == null || cloudCat == null) continue;

                var match = cloudInvs.FirstOrDefault(i => string.Equals(i.SerialNumber, li.SerialNumber, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.Inventories.Add(new Inventory
                    {
                        DeviceName = li.DeviceName,
                        DeviceCategoryId = cloudCat.Id,
                        SerialNumber = li.SerialNumber,
                        BatchCode = li.BatchCode,
                        Status = li.Status,
                        DateReceived = li.DateReceived,
                        PurchaseDate = li.PurchaseDate,
                        PurchasedFrom = li.PurchasedFrom,
                        PurchaseCost = li.PurchaseCost,
                        ProcurementQuantity = li.ProcurementQuantity,
                        Notes = li.Notes,
                        HasStorageDevice = li.HasStorageDevice,
                        RecordedByUsername = li.RecordedByUsername,
                        BranchId = cloudBranch.Id
                    });
                    count++;
                }
                else
                {
                    match.Status = li.Status;
                    match.Notes = li.Notes;
                }
            }
            await cloudTenant.SaveChangesAsync();

            // 6. Sync CommoditySales
            var localSales = await localTenant.CommoditySales.AsNoTracking().ToListAsync();
            var cloudSales = await cloudTenant.CommoditySales.ToListAsync();
            foreach (var ls in localSales)
            {
                var localBranch = localBranches.FirstOrDefault(b => b.Id == ls.BranchId);
                var cloudBranch = localBranch != null
                    ? refreshedCloudBranches.FirstOrDefault(b => string.Equals(b.Code, localBranch.Code, StringComparison.OrdinalIgnoreCase))
                    : refreshedCloudBranches.FirstOrDefault();

                if (cloudBranch == null) continue;

                var match = cloudSales.FirstOrDefault(s => string.Equals(s.InvoiceNumber, ls.InvoiceNumber, StringComparison.OrdinalIgnoreCase));
                if (match == null)
                {
                    cloudTenant.CommoditySales.Add(new CommoditySale
                    {
                        MaterialName = ls.MaterialName,
                        BuyerName = ls.BuyerName,
                        QuantityKg = ls.QuantityKg,
                        PricePerKg = ls.PricePerKg,
                        TotalAmount = ls.TotalAmount,
                        InvoiceNumber = ls.InvoiceNumber,
                        SaleDate = ls.SaleDate,
                        Notes = ls.Notes,
                        BranchId = cloudBranch.Id
                    });
                    count++;
                }
            }
            await cloudTenant.SaveChangesAsync();

            // 7. Sync CompanyFinanceTransactions
            var localFin = await localTenant.CompanyFinanceTransactions.AsNoTracking().ToListAsync();
            var cloudFin = await cloudTenant.CompanyFinanceTransactions.ToListAsync();
            foreach (var lf in localFin)
            {
                var localBranch = localBranches.FirstOrDefault(b => b.Id == lf.BranchId);
                var cloudBranch = localBranch != null
                    ? refreshedCloudBranches.FirstOrDefault(b => string.Equals(b.Code, localBranch.Code, StringComparison.OrdinalIgnoreCase))
                    : refreshedCloudBranches.FirstOrDefault();

                var match = !string.IsNullOrWhiteSpace(lf.SourceReference)
                    ? cloudFin.FirstOrDefault(f => string.Equals(f.SourceReference, lf.SourceReference, StringComparison.OrdinalIgnoreCase))
                    : cloudFin.FirstOrDefault(f => f.TransactionDate == lf.TransactionDate && f.Amount == lf.Amount && f.Description == lf.Description);

                if (match == null && cloudBranch != null)
                {
                    cloudTenant.CompanyFinanceTransactions.Add(new CompanyFinanceTransaction
                    {
                        Type = lf.Type,
                        Category = lf.Category,
                        Amount = lf.Amount,
                        TransactionDate = lf.TransactionDate,
                        Description = lf.Description,
                        SourceReference = lf.SourceReference,
                        RecordedByUserId = lf.RecordedByUserId,
                        BranchId = cloudBranch.Id
                    });
                    count++;
                }
                else if (match != null)
                {
                    match.Amount = lf.Amount;
                    match.Type = lf.Type;
                    match.Category = lf.Category;
                    match.Description = lf.Description;
                    if (cloudBranch != null) match.BranchId = cloudBranch.Id;
                }
            }
            await cloudTenant.SaveChangesAsync();

            result.Details.Add($"Tenant {companyCode}: Successfully synced {count} records into cloud.");
            return count;
        }

        private async Task EnsureMasterSchemaAsync(MasterErpDbContext cloudMaster)
        {
            try
            {
                await cloudMaster.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "EnsureCreatedAsync on cloud master threw an exception");
            }

            const string sql = @"
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Companies')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'ContactEmail')
        ALTER TABLE Companies ADD ContactEmail NVARCHAR(255) NULL;

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'ContactPhone')
        ALTER TABLE Companies ADD ContactPhone NVARCHAR(50) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'SubscriptionPlan')
    ALTER TABLE Companies ADD SubscriptionPlan NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'SubscriptionExpiresAt')
    ALTER TABLE Companies ADD SubscriptionExpiresAt DATETIME2 NULL;

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'EnabledModules')
    ALTER TABLE Companies ADD EnabledModules NVARCHAR(MAX) NULL;
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CompanyDatabases')
BEGIN
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CompanyDatabases' AND COLUMN_NAME = 'DatabaseType')
        ALTER TABLE CompanyDatabases ADD DatabaseType NVARCHAR(20) NOT NULL DEFAULT 'Local';

    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'CompanyDatabases' AND COLUMN_NAME = 'ConnectionString')
        ALTER TABLE CompanyDatabases ADD ConnectionString NVARCHAR(1000) NULL;
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionHistories')
BEGIN
    CREATE TABLE SubscriptionHistories (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        CompanyId INT NOT NULL,
        PlanName NVARCHAR(100) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        BillingCycle NVARCHAR(50) NOT NULL DEFAULT 'Monthly',
        StartDate DATETIME2 NOT NULL,
        EndDate DATETIME2 NOT NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Active',
        Notes NVARCHAR(MAX) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SuperAdmins')
BEGIN
    CREATE TABLE SuperAdmins (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Username NVARCHAR(100) NOT NULL,
        PasswordHash NVARCHAR(MAX) NOT NULL,
        FullName NVARCHAR(200) NOT NULL,
        Email NVARCHAR(200) NOT NULL,
        IsActive BIT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );
END
";
            try
            {
                await cloudMaster.Database.ExecuteSqlRawAsync(sql);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while ensuring cloud master schema.");
            }
        }

        private async Task EnsureTenantSchemaAsync(TenantErpDbContext cloudTenant)
        {
            const string cleanupSql = @"
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Branches')
BEGIN
    DROP TABLE IF EXISTS Products;
    DROP TABLE IF EXISTS __EFMigrationsHistory;
END
";
            try
            {
                await cloudTenant.Database.ExecuteSqlRawAsync(cleanupSql);
            }
            catch { }

            try
            {
                await cloudTenant.Database.EnsureCreatedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error executing EnsureCreatedAsync on cloud tenant database.");
            }
        }

        private async Task<bool> TestConnectionAsync(string connStr)
        {
            try
            {
                var builder = new SqlConnectionStringBuilder(connStr)
                {
                    ConnectTimeout = 30
                };
                await using var conn = new SqlConnection(builder.ConnectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TestConnectionAsync failed for cloud connection.");
                return false;
            }
        }
    }
}
