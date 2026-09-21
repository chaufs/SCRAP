using System;

namespace SCRAP.domain.entities
{
    // Maps a device category to expected raw material yield per one unit
    public class ArchetypeRecipe
    {
        public int Id { get; set; }
        public int DeviceCategoryId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        // kilograms of material expected per single device
        public decimal WeightKgPerUnit { get; set; }

        // Navigation
        public DeviceCategory? DeviceCategory { get; set; }
    }
}
