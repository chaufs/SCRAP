using System.Threading.Tasks;
using SCRAP.domain.entities;

namespace SCRAP.infrastructure.services
{
    public interface ICloudSyncService
    {
        Task<CloudSyncStatus> GetSyncStatusAsync();
        Task<CloudSyncResult> SyncAllAsync();
        Task<CloudSyncResult> SyncTenantAsync(string companyCode);
    }
}
