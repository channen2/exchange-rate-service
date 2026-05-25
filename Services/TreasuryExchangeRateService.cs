using System.Text.Json;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Configuration;
using ExchangeRateService.DTOs.Treasury;
using ExchangeRateService.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace ExchangeRateService.Services
{
    public class TreasuryExchangeRateService : ITreasuryExchangeRateService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _currencyMappings;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public TreasuryExchangeRateService(
            HttpClient httpClient,
            IOptions<TreasuryCurrencyOptions> options
        )
        {
            _httpClient = httpClient;
            _currencyMappings = options.Value.CurrencyMappings;
        }

        public async Task<Result<decimal>> GetExchangeRateAsync(
            string targetCurrency,
            DateTime transactionDate
        )
        {
            if (
                !_currencyMappings.TryGetValue(
                    targetCurrency.ToUpperInvariant(),
                    out string? treasuryCurrency
                )
            )
            {
                return Result<decimal>.Failure(
                    Errors.UnsupportedCurrency,
                    new Dictionary<string, object> { ["currency"] = targetCurrency }
                );
            }

            DateTime fromDate = transactionDate.AddMonths(-6);

            string url =
                "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange"
                + $"?filter=record_date:gte:{fromDate:yyyy-MM-dd}"
                + $",record_date:lte:{transactionDate:yyyy-MM-dd}";

            HttpResponseMessage response = await _httpClient.GetAsync(url);

            _ = response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();

            TreasuryExchangeRateApiResponse? apiResponse =
                JsonSerializer.Deserialize<TreasuryExchangeRateApiResponse>(json, JsonOptions);

            if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            {
                return Result<decimal>.Failure(
                    Errors.ExchangeRateNotFound,
                    new Dictionary<string, object>
                    {
                        ["currency"] = targetCurrency,
                        ["transactionDate"] = transactionDate.ToString("yyyy-MM-dd"),
                        ["searchWindow"] = $"{fromDate:yyyy-MM-dd} to {transactionDate:yyyy-MM-dd}",
                    }
                );
            }

            var candidates = apiResponse
                .Data.Where(x =>
                    x.CountryCurrencyDescription.Equals(
                        treasuryCurrency,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                .Select(x => new { Record = x, Date = DateTime.Parse(x.RecordDate) })
                .Where(x => x.Date <= transactionDate)
                .Where(x => x.Date >= transactionDate.AddMonths(-6))
                .OrderByDescending(x => x.Date)
                .ToList();

            TreasuryExchangeRateRecord? bestMatch = candidates.FirstOrDefault()?.Record;

            if (bestMatch is null)
            {
                return Result<decimal>.Failure(
                    Errors.ExchangeRateNotFound,
                    new Dictionary<string, object>
                    {
                        ["currency"] = targetCurrency,
                        ["transactionDate"] = transactionDate.ToString("yyyy-MM-dd"),
                        ["searchWindow"] = $"{fromDate:yyyy-MM-dd} to {transactionDate:yyyy-MM-dd}",
                    }
                );
            }

            if (!decimal.TryParse(bestMatch.ExchangeRate, out var rate))
            {
                return Result<decimal>.Failure(
                    Errors.ExchangeRateParseError,
                    new Dictionary<string, object>
                    {
                        ["currency"] = targetCurrency,
                        ["exchangeRateValue"] = bestMatch.ExchangeRate,
                    }
                );
            }

            return Result<decimal>.Success(rate);
        }
    }
}
