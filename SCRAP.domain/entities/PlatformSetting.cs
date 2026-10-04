using System;

namespace SCRAP.domain.entities
{
    /// <summary>
    /// Key-value store for platform-wide configuration managed by SuperAdmin.
    /// Keys: "TermsAndConditions", etc.
    /// </summary>
    public class PlatformSetting
    {
        public int Id { get; set; }

        /// <summary>Setting key (unique identifier, e.g. "TermsAndConditions").</summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>Setting value (nvarchar(max) for large text blobs).</summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>Last update timestamp (UTC).</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Username of the last SuperAdmin who updated this setting.</summary>
        public string UpdatedBy { get; set; } = "superadmin";
    }
}
