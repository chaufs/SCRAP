using System.Threading.Tasks;

namespace SCRAP.infrastructure.services
{
    public interface ITenantDatabaseResolver
    {
        Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
        Task<TenantDatabaseInfo> GetDatabaseInfoByCodeAsync(string companyCode);

        /// <summary>
        /// Get the cloud database info for a tenant from MasterDB.
        /// Returns null if no cloud database record exists.
        /// </summary>
        Task<TenantDatabaseInfo?> GetCloudDatabaseInfoByCodeAsync(string companyCode);
    }
}
