using System.ComponentModel.DataAnnotations;

namespace ExchangeRateService.DTOs
{
    public class CreateTransactionRequest
    {
        [Required]
        [StringLength(50)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal PurchaseAmountUsd { get; set; }

        [Required]
        public DateTime? TransactionDate { get; set; }
    }
}
