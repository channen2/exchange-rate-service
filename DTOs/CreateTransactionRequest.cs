using System.ComponentModel.DataAnnotations;

namespace ExchangeRateService.DTOs
{
    public class CreateTransactionRequest
    {
        [Required(ErrorMessage = "The Description field is required")]
        [StringLength(50, ErrorMessage = "Description cannot exceed 50 characters.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "The Purchase amount field is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Purchase amount must be greater than 0.")]
        public decimal PurchaseAmountUsd { get; set; }

        [Required(ErrorMessage = "The Transaction date field is required")]
        public DateTime? TransactionDate { get; set; }
    }
}
