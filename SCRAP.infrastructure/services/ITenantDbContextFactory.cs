using System.Threading.Tasks;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure.services
{
    public interface ITenantDbContextFactory
    {
        Task<TenantErpDbContext> CreateAsync(int companyId);
        Task<TenantErpDbContext> CreateByCodeAsync(string companyCode);
        Task<string> GetConnectionStringAsync(int companyId);
        Task<string> GetConnectionStringByCodeAsync(string companyCode);

        /// <summary>
        /// Get cloud connection string for a tenant, resolved from MasterDB first, fallback to config.
        /// Returns null if no cloud database is configured for this tenant.
        /// </summary>
        Task<string?> GetCloudConnectionStringByCodeAsync(string companyCode);

        TenantErpDbContext CreateFromConnectionString(string connectionString);
    }
}
