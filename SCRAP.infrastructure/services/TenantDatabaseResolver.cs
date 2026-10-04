using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure.services
{
    public class TenantDatabaseResolver : ITenantDatabaseResolver
    {
        private readonly MasterErpDbContext _masterDb;

        public TenantDatabaseResolver(MasterErpDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        /// <summary>
        /// Resolve the LOCAL database info for a tenant by CompanyId.
        /// Always goes through MasterDB first.
        /// </summary>
        public async Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId)
        {
            var tenantDatabase = await _masterDb.CompanyDatabases
                .Include(x => x.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId
                                       && x.IsActive
                                       && (x.DatabaseType == "Local" || x.DatabaseType == null));

            if (tenantDatabase == null)
            {
                throw new InvalidOperationException($"No active local tenant database found for CompanyId {companyId}.");
            }

            return MapToInfo(tenantDatabase);
        }

        /// <summary>
        /// Resolve the LOCAL database info for a tenant by CompanyCode.
        /// Always goes through MasterDB first.
        /// </summary>
        public async Task<TenantDatabaseInfo> GetDatabaseInfoByCodeAsync(string companyCode)
        {
            var company = await _masterDb.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode);

            if (company == null)
            {
                throw new InvalidOperationException($"Subscriber company '{companyCode}' not found.");
            }

            if (company.SubscriptionExpiresAt.HasValue && company.SubscriptionExpiresAt.Value < DateTime.UtcNow)
            {
                throw new InvalidOperationException($"Subscription Expired: The subscription for '{company.CompanyName}' expired on {company.SubscriptionExpiresAt.Value:MMM dd, yyyy} and has been deactivated.");
            }

            if (!company.IsActive)
            {
                throw new InvalidOperationException($"Subscriber company '{company.CompanyName}' is suspended.");
            }

            return await GetDatabaseInfoAsync(company.CompanyId);
        }

        /// <summary>
        /// Resolve the CLOUD database info for a tenant by CompanyCode from MasterDB.
        /// Returns null if no cloud database record exists for this tenant.
        /// </summary>
        public async Task<TenantDatabaseInfo?> GetCloudDatabaseInfoByCodeAsync(string companyCode)
        {
            var company = await _masterDb.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyCode == companyCode);

            if (company == null) return null;

            var cloudDb = await _masterDb.CompanyDatabases
                .Include(x => x.Company)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == company.CompanyId
                                       && x.DatabaseType == "Cloud");

            if (cloudDb == null) return null;

            return MapToInfo(cloudDb);
        }

        private static TenantDatabaseInfo MapToInfo(domain.entities.CompanyDatabase db)
        {
            return new TenantDatabaseInfo
            {
                CompanyId = db.CompanyId,
                CompanyCode = db.Company?.CompanyCode ?? "",
                ServerName = db.ServerName,
                DatabaseName = db.DatabaseName,
                CredentialKey = db.CredentialKey,
                DatabaseType = db.DatabaseType ?? "Local",
                ConnectionString = db.ConnectionString
            };
        }
    }
}