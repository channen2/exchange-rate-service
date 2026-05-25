namespace ExchangeRateService.DTOs.Responses
{
    public class ConvertedTransactionResponse
    {
        public Guid Id { get; set; }

        public string Description { get; set; } = string.Empty;

        public DateTime TransactionDate { get; set; }

        public decimal OriginalPurchaseAmountUsd { get; set; }

        public decimal ExchangeRate { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public decimal ConvertedAmount { get; set; }
    }
}
