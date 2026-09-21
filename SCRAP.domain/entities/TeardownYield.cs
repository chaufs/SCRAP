namespace SCRAP.domain.entities
{
    public class TeardownYield
    {
        public int Id { get; set; }
        public int TeardownBatchId { get; set; }
        public string MaterialName { get; set; } = string.Empty;
        public decimal WeightKg { get; set; }

        public TeardownBatch? TeardownBatch { get; set; }
    }
}