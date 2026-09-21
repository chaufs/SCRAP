using SCRAP.infrastructure.data;
using System;
using System.Collections.Generic;
using System.Text;

namespace SCRAP.infrastructure.services
{
    public interface ITenantDbContextFactory
    {
        Task<TenantErpDbContext> CreateAsync(int companyId);


    }
}
