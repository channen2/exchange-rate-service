using ExchangeRateService.Configuration;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExchangeRateService.Services
{
    public class CurrencyService(IOptions<TreasuryCurrencyOptions> options) : ICurrencyService
    {
        private readonly TreasuryCurrencyOptions _options = options.Value;

        public IReadOnlyList<SupportedCurrencyResponse> GetSupportedCurrencies()
        {
            return _options
                .CurrencyMappings.Select(x => new SupportedCurrencyResponse(x.Key, x.Value))
                .OrderBy(x => x.Code)
                .ToList();
        }
    }
}
