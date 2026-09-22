using System;

namespace SCRAP.domain.entities
{
    public enum FinanceTransactionType
    {
        Income,
        Deduction
    }

    public class CompanyFinanceTransaction
    {
        public int Id { get; set; }
        public FinanceTransactionType Type { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; } = string.Empty;
        public string? SourceReference { get; set; }
        public int? RecordedByUserId { get; set; }
    }
}
