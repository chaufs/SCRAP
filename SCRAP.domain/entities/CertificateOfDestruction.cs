using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities
{
    public enum DestructionMethod
    {
        PhysicalShredding = 0,
        Crushing = 1,
        Degaussing = 2,
        Incineration = 3,
        Disintegration = 4
    }

    public class CertificateOfDestruction
    {
        public int Id { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime DestructionDateTime { get; set; }

        // Party details
        public string OrganizationName { get; set; } = string.Empty;
        public string OrganizationAddress { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderAddress { get; set; } = string.Empty;

        // Destruction & compliance
        public DestructionMethod Method { get; set; }
        public string SecurityStandard { get; set; } = string.Empty; // e.g. "NIST SP 800-88 Rev.1"
        public string? SoftwareToolName { get; set; }
        public string? SoftwareToolVersion { get; set; }

        // Verification / sign-off
        public int ManagerUserId { get; set; }
        public string VerifiedByName { get; set; } = string.Empty;
        public DateTime VerifiedDate { get; set; }
        public string? Notes { get; set; }

        public ICollection<CertificateOfDestructionItem> Items { get; set; } = new List<CertificateOfDestructionItem>();
    }
}