using System;
using System.Collections.Generic;

namespace SCRAP.domain.entities
{
    public class PlatformSummaryReport
    {
        public int TotalSubscribers { get; set; }
        public int ActiveSubscribers { get; set; }
        public int SuspendedSubscribers { get; set; }
        public int BasicCount { get; set; }
        public int StandardCount { get; set; }
        public int EnterpriseCount { get; set; }
        public int CustomCount { get; set; }
        public decimal EstimatedMonthlyRevenue { get; set; }
        public decimal TotalCollectedRevenue { get; set; }

        // BI Analytics
        public decimal EstimatedAnnualRevenue => EstimatedMonthlyRevenue * 12;
        public decimal RetentionRate => TotalSubscribers > 0 ? Math.Round((decimal)ActiveSubscribers / TotalSubscribers * 100m, 1) : 100m;
        public int TotalPlatformUsers { get; set; }
        public int TotalPlatformBranches { get; set; }
        public int CloudDatabasesCount { get; set; }
        public decimal CloudAdoptionRate => TotalSubscribers > 0 ? Math.Round((decimal)CloudDatabasesCount / TotalSubscribers * 100m, 1) : 0m;
        public int ExpiringSoonCount { get; set; }

        public List<ModuleAdoptionDto> ModuleAdoptions { get; set; } = new();
        public List<MonthlyTrendPointDto> RevenueTrends { get; set; } = new();
        public List<TenantReportItemDto> Tenants { get; set; } = new();
        public List<SubscriptionHistoryReportDto> SubscriptionHistories { get; set; } = new();
    }

    public class ModuleAdoptionDto
    {
        public string ModuleName { get; set; } = string.Empty;
        public int TenantCount { get; set; }
        public decimal AdoptionPercentage { get; set; }
    }

    public class MonthlyTrendPointDto
    {
        public string MonthLabel { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TenantReportItemDto
    {
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? ContactEmail { get; set; }
        public string? SubscriptionPlan { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? SubscriptionExpiresAt { get; set; }
        public string? EnabledModules { get; set; }
        public string? DatabaseName { get; set; }
        public string? ServerName { get; set; }
        public string? CloudDatabaseName { get; set; }
        public string? CloudServerName { get; set; }
        public decimal TotalMoneyGenerated { get; set; }
        public int UserCount { get; set; }
        public int BranchCount { get; set; }

        public string StatusText => IsActive ? "Active" : "Suspended";
        public string HealthScore => !IsActive ? "Suspended" : (DaysRemaining.HasValue && DaysRemaining.Value <= 30 ? "Expiring Soon" : "Healthy");
        public string MonthlyRateFormatted => (SubscriptionPlan?.ToLowerInvariant()) switch
        {
            "basic" => "₱99.00",
            "standard" => "₱249.00",
            "enterprise" => "₱599.00",
            _ => "₱199.00"
        };
        public string TotalMoneyGeneratedFormatted => $"₱{TotalMoneyGenerated:N2}";
        public int? DaysRemaining => SubscriptionExpiresAt.HasValue
            ? (int)Math.Max(0, (SubscriptionExpiresAt.Value.Date - DateTime.UtcNow.Date).TotalDays)
            : null;
    }

    public class SubscriptionHistoryReportDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public string? PlanName { get; set; }
        public decimal Amount { get; set; }
        public string AmountFormatted => $"₱{Amount:N2}";
        public string? BillingCycle { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string PeriodFormatted => $"{StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}";
    }
}
