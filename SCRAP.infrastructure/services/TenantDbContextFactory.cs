using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SCRAP.infrastructure.data;

namespace SCRAP.infrastructure.services
{
    public class TenantDbContextFactory : ITenantDbContextFactory
    {
        private readonly ITenantDatabaseResolver _resolver;
        private readonly IConfiguration _configuration;

        public TenantDbContextFactory(
            ITenantDatabaseResolver resolver,
            IConfiguration configuration)
        {
            _resolver = resolver;
            _configuration = configuration;
        }

        public async Task<string> GetConnectionStringAsync(int companyId)
        {
            var databaseInfo = await _resolver.GetDatabaseInfoAsync(companyId);
            return BuildConnectionString(databaseInfo);
        }

        public async Task<string> GetConnectionStringByCodeAsync(string companyCode)
        {
            var databaseInfo = await _resolver.GetDatabaseInfoByCodeAsync(companyCode);
            return BuildConnectionString(databaseInfo);
        }

        /// <summary>
        /// Get the cloud connection string for a tenant, resolved from MasterDB first,
        /// with fallback to appsettings.json config.
        /// </summary>
        public async Task<string?> GetCloudConnectionStringByCodeAsync(string companyCode)
        {
            // 1. Try MasterDB first
            var cloudInfo = await _resolver.GetCloudDatabaseInfoByCodeAsync(companyCode);
            if (cloudInfo != null)
            {
                // If a full ConnectionString is stored, use it directly
                if (!string.IsNullOrWhiteSpace(cloudInfo.ConnectionString))
                    return cloudInfo.ConnectionString;

                // Otherwise build from ServerName/DatabaseName/CredentialKey
                return BuildConnectionString(cloudInfo);
            }

            // 2. Fallback to appsettings.json config (backward compatibility)
            return _configuration[$"CloudSync:TenantCloudConnections:{companyCode}"]
                ?? _configuration.GetConnectionString($"CloudTenant_{companyCode}");
        }

        public async Task<TenantErpDbContext> CreateAsync(int companyId)
        {
            var connStr = await GetConnectionStringAsync(companyId);
            return CreateFromConnectionString(connStr);
        }

        public async Task<TenantErpDbContext> CreateByCodeAsync(string companyCode)
        {
            var connStr = await GetConnectionStringByCodeAsync(companyCode);
            return CreateFromConnectionString(connStr);
        }

        public TenantErpDbContext CreateFromConnectionString(string connectionString)
        {
            var options = new DbContextOptionsBuilder<TenantErpDbContext>()
                .UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
                .Options;

            return new TenantErpDbContext(options);
        }

        private string BuildConnectionString(TenantDatabaseInfo databaseInfo)
        {
            // 0. If a full connection string is stored directly, use it
            if (!string.IsNullOrWhiteSpace(databaseInfo.ConnectionString))
            {
                return databaseInfo.ConnectionString;
            }

            // 1. Check if direct full connection string is defined for this database/credential in config
            var directConn = _configuration.GetConnectionString(databaseInfo.DatabaseName)
                             ?? _configuration.GetConnectionString(databaseInfo.CredentialKey);
            if (!string.IsNullOrWhiteSpace(directConn))
            {
                return directConn;
            }

            // 2. Check credentials configuration
            var userId = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:UserId"];
            var password = _configuration[$"TenantCredentials:{databaseInfo.CredentialKey}:Password"];

            // 3. Fallback: Parse from MasterErp connection string credentials if on same server
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                var masterConn = _configuration.GetConnectionString("MasterErp") ?? "";
                var masterBuilder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(masterConn);
                if (!string.IsNullOrWhiteSpace(masterBuilder.UserID))
                {
                    userId = masterBuilder.UserID;
                    password = masterBuilder.Password;
                }
            }

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(password))
            {
                // Fall back to trusted connection / Windows Auth
                return $"Server={databaseInfo.ServerName};Database={databaseInfo.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
            }

            return $"Server={databaseInfo.ServerName};Database={databaseInfo.DatabaseName};User Id={userId};Password={password};Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
        }
    }
}
