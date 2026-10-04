using System;
using System.Collections.Generic;
using System.Text;

namespace SCRAP.domain.entities;

public class CompanyDatabase
{
    public int CompanyDatabaseId { get; set; }

    public int CompanyId { get; set; }

    public string ServerName { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public string CredentialKey { get; set; } = string.Empty;

    /// <summary>
    /// "Local" or "Cloud". Defaults to "Local" for backward compatibility.
    /// </summary>
    public string DatabaseType { get; set; } = "Local";

    /// <summary>
    /// Full connection string (primarily used for Cloud databases with unique per-tenant credentials).
    /// When populated, this takes priority over ServerName/DatabaseName/CredentialKey resolution.
    /// </summary>
    public string? ConnectionString { get; set; }

    public Company? Company { get; set; }
}
