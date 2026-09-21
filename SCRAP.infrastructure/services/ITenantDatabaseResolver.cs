using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
namespace SCRAP.infrastructure.services
{
    public interface ITenantDatabaseResolver
    {
        Task<TenantDatabaseInfo> GetDatabaseInfoAsync(int companyId);
    }
}
