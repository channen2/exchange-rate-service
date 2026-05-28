using ExchangeRateService.Models;

namespace ExchangeRateService.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<PurchaseTransaction> CreateAsync(
            decimal amount,
            DateTime transactionDate,
            string description
        );
        Task<List<PurchaseTransaction>> GetAllAsync();
        Task<PurchaseTransaction?> GetByIdAsync(Guid id);
    }
}
