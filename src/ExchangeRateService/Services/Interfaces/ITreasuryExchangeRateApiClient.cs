using ExchangeRateService.Common;
using ExchangeRateService.DTOs.Treasury;

namespace ExchangeRateService.Services.Interfaces
{
    public interface ITreasuryExchangeRateApiClient
    {
        Task<Result<TreasuryExchangeRateApiResponse>> GetExchangeRatesAsync(
            DateTime fromDate,
            DateTime toDate,
            string? treasuryCurrency
        );
    }
}
