using ExchangeRateService.Common;

namespace ExchangeRateService.Services.Interfaces
{
    public interface IExchangeRateProvider
    {
        Task<Result<decimal>> GetRateAsync(string treasuryCurrency, DateTime transactionDate);
    }
}
