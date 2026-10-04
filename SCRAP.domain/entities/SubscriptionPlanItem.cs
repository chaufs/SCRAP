using System;

namespace SCRAP.domain.entities
{
    public class SubscriptionPlanItem
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string BillingCycle { get; set; } = "month";
        public string Description { get; set; } = string.Empty;
        public string EnabledModules { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#2563EB";
        public bool IsRecommended { get; set; }
    }
}
