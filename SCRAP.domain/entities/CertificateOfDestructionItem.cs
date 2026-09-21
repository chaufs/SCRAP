namespace SCRAP.domain.entities
{
    public class CertificateOfDestructionItem
    {
        public int Id { get; set; }
        public int CertificateOfDestructionId { get; set; }
        public int StorageDestructionRecordId { get; set; }

        public string SerialNumber { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty; // category name
        public string Model { get; set; } = string.Empty;      // device name

        public CertificateOfDestruction? CertificateOfDestruction { get; set; }
    }
}