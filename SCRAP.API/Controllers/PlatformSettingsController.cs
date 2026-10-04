using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCRAP.domain.entities;
using SCRAP.infrastructure.data;
using System;
using System.Threading.Tasks;

namespace SCRAP.API.Controllers
{
    /// <summary>
    /// Manages platform-wide settings stored in the master database.
    /// All endpoints require SuperAdmin authentication (X-Company-Code: MASTER).
    /// </summary>
    [ApiController]
    [Route("api/platform/settings")]
    public class PlatformSettingsController : ControllerBase
    {
        private readonly MasterErpDbContext _masterDb;
        private const string TermsKey = "TermsAndConditions";

        public PlatformSettingsController(MasterErpDbContext masterDb)
        {
            _masterDb = masterDb;
        }

        // ─── GET api/platform/settings/terms ─────────────────────────────────────
        /// <summary>Returns the current Terms & Conditions text. Public read (any role).</summary>
        [HttpGet("terms")]
        public async Task<IActionResult> GetTerms()
        {
            var setting = await _masterDb.PlatformSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == TermsKey);

            if (setting == null)
            {
                // Return empty so the client can show the default hard-coded text
                return Ok(new TermsDto { Value = string.Empty, UpdatedAt = null, UpdatedBy = null });
            }

            return Ok(new TermsDto
            {
                Value = setting.Value,
                UpdatedAt = setting.UpdatedAt,
                UpdatedBy = setting.UpdatedBy
            });
        }

        // ─── PUT api/platform/settings/terms ─────────────────────────────────────
        /// <summary>Upserts the Terms & Conditions text. SuperAdmin only.</summary>
        [HttpPut("terms")]
        public async Task<IActionResult> UpdateTerms([FromBody] UpdateTermsRequest request)
        {
            // Validate caller is SuperAdmin
            var companyCode = Request.Headers["X-Company-Code"].ToString();
            var apiUser = Request.Headers["X-Api-User"].ToString();

            bool isSuperAdmin = companyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase)
                || apiUser.StartsWith("superadmin", StringComparison.OrdinalIgnoreCase)
                || (User?.IsInRole("Superadmin") ?? false)
                || (User?.IsInRole("SuperAdmin") ?? false);

            if (!isSuperAdmin)
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(request?.Value))
                return BadRequest(new { error = "Terms content cannot be empty." });

            var effectiveUser = !string.IsNullOrWhiteSpace(apiUser) ? apiUser : "superadmin";

            var existing = await _masterDb.PlatformSettings
                .FirstOrDefaultAsync(x => x.Key == TermsKey);

            if (existing == null)
            {
                _masterDb.PlatformSettings.Add(new PlatformSetting
                {
                    Key = TermsKey,
                    Value = request.Value.Trim(),
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = effectiveUser
                });
            }
            else
            {
                existing.Value = request.Value.Trim();
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = effectiveUser;
            }

            await _masterDb.SaveChangesAsync();
            return Ok(new { message = "Terms & Conditions updated successfully." });
        }

        // ─── GET api/platform/settings/plans ─────────────────────────────────────
        /// <summary>Returns the current subscription plans list. Public/SuperAdmin read.</summary>
        [HttpGet("plans")]
        public async Task<IActionResult> GetPlans()
        {
            var setting = await _masterDb.PlatformSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Key == PlansKey);

            if (setting == null || string.IsNullOrWhiteSpace(setting.Value))
            {
                return Ok(GetDefaultPlans());
            }

            try
            {
                var plans = System.Text.Json.JsonSerializer.Deserialize<List<SubscriptionPlanItem>>(
                    setting.Value, 
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (plans != null && plans.Count > 0)
                {
                    return Ok(plans);
                }
            }
            catch
            {
                // Fall back to defaults if parsing fails
            }

            return Ok(GetDefaultPlans());
        }

        // ─── PUT api/platform/settings/plans ─────────────────────────────────────
        /// <summary>Updates subscription plan definitions. SuperAdmin only.</summary>
        [HttpPut("plans")]
        public async Task<IActionResult> UpdatePlans([FromBody] List<SubscriptionPlanItem> plans)
        {
            var companyCode = Request.Headers["X-Company-Code"].ToString();
            var apiUser = Request.Headers["X-Api-User"].ToString();

            bool isSuperAdmin = companyCode.Equals("MASTER", StringComparison.OrdinalIgnoreCase)
                || apiUser.StartsWith("superadmin", StringComparison.OrdinalIgnoreCase)
                || (User?.IsInRole("Superadmin") ?? false)
                || (User?.IsInRole("SuperAdmin") ?? false);

            if (!isSuperAdmin)
            {
                return Forbid();
            }

            if (plans == null || plans.Count == 0)
                return BadRequest(new { error = "Plans list cannot be empty." });

            var effectiveUser = !string.IsNullOrWhiteSpace(apiUser) ? apiUser : "superadmin";

            var json = System.Text.Json.JsonSerializer.Serialize(plans, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

            var existing = await _masterDb.PlatformSettings
                .FirstOrDefaultAsync(x => x.Key == PlansKey);

            if (existing == null)
            {
                _masterDb.PlatformSettings.Add(new PlatformSetting
                {
                    Key = PlansKey,
                    Value = json,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = effectiveUser
                });
            }
            else
            {
                existing.Value = json;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.UpdatedBy = effectiveUser;
            }

            // Synchronize updated module permissions to all subscriber companies on these plans
            foreach (var p in plans)
            {
                if (string.IsNullOrWhiteSpace(p.Name)) continue;
                var companiesOnPlan = await _masterDb.Companies
                    .Where(c => c.SubscriptionPlan != null && c.SubscriptionPlan.ToLower() == p.Name.ToLower())
                    .ToListAsync();

                foreach (var c in companiesOnPlan)
                {
                    c.EnabledModules = p.EnabledModules;
                }
            }

            await _masterDb.SaveChangesAsync();
            return Ok(new { message = "Subscription plans updated successfully." });
        }

        public static List<SubscriptionPlanItem> GetDefaultPlans()
        {
            return new List<SubscriptionPlanItem>
            {
                new SubscriptionPlanItem
                {
                    Name = "Basic",
                    DisplayName = "Basic Tier",
                    Price = 99.00m,
                    BillingCycle = "month",
                    Description = "Core operations for scrap intake, sales, teardown, and inventory.",
                    EnabledModules = "Inventory,Procurement,Sales,Technical,Reports",
                    ColorHex = "#2563EB",
                    IsRecommended = false
                },
                new SubscriptionPlanItem
                {
                    Name = "Standard",
                    DisplayName = "Standard Tier (Recommended)",
                    Price = 249.00m,
                    BillingCycle = "month",
                    Description = "Best for growing multi-depot e-waste operations.",
                    EnabledModules = "Inventory,Procurement,Sales,Technical,HR,Branches,Reports",
                    ColorHex = "#16A34A",
                    IsRecommended = true
                },
                new SubscriptionPlanItem
                {
                    Name = "Enterprise",
                    DisplayName = "Enterprise Tier",
                    Price = 599.00m,
                    BillingCycle = "month",
                    Description = "Complete solution for full-scale industrial operations.",
                    EnabledModules = "Inventory,Procurement,Sales,Technical,HR,Branches,Reports,Finance",
                    ColorHex = "#4F46E5",
                    IsRecommended = false
                }
            };
        }

        private const string PlansKey = "SubscriptionPlanDefinitions";
    }

    // ─── DTOs ─────────────────────────────────────────────────────────────────────

    public class TermsDto
    {
        public string Value { get; set; } = string.Empty;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class UpdateTermsRequest
    {
        public string Value { get; set; } = string.Empty;
    }
}
