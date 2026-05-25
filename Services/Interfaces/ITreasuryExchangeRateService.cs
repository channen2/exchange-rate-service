using ExchangeRateService.Common;

namespace ExchangeRateService.Services.Interfaces
{
    public interface ITreasuryExchangeRateService
    {
        Task<Result<decimal>> GetExchangeRateAsync(string targetCurrency, DateTime transactionDate);
    }
}
