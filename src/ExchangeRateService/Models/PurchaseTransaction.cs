namespace ExchangeRateService.Models
{
    public class PurchaseTransaction
    {
        public Guid Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public decimal PurchaseAmountUsd { get; set; }

        public DateTime TransactionDate { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
