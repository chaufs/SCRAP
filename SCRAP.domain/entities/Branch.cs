namespace SCRAP.domain.entities
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name    { get; set; } = string.Empty;  // "Main Branch"
        public string Code    { get; set; } = string.Empty;  // "BR-001"
        public string? Address { get; set; }
        public bool IsActive  { get; set; } = true;

        // Navigation
        [System.Text.Json.Serialization.JsonIgnore]
        public ICollection<UserManagement>? Users { get; set; }
    }
}
