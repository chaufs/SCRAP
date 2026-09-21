namespace SCRAP.domain.entities
{
    public class DeviceCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation
        public System.Collections.Generic.List<Inventory>? InventoryItems { get; set; }
    }
}