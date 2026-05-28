using ExchangeRateService.DTOs.Responses;

namespace ExchangeRateService.Services.Interfaces
{
    public interface ICurrencyService
    {
        IReadOnlyList<SupportedCurrencyResponse> GetSupportedCurrencies();
    }
}
