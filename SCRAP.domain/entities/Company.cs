using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities;

public class Company
{
    public int CompanyId { get; set; }
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string SubscriptionPlan { get; set; } = "Standard";
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public string EnabledModules { get; set; } = "Inventory,Sales,Technical";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Device> Devices { get; set; } = new List<Device>();
}
