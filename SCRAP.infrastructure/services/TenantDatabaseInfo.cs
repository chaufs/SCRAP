namespace SCRAP.infrastructure.services
{
    public class TenantDatabaseInfo
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string CredentialKey { get; set; } = string.Empty;

        /// <summary>
        /// "Local" or "Cloud"
        /// </summary>
        public string DatabaseType { get; set; } = "Local";

        /// <summary>
        /// Full connection string (if stored directly in MasterDB, takes priority over ServerName/DatabaseName/CredentialKey).
        /// </summary>
        public string? ConnectionString { get; set; }
    }
}
