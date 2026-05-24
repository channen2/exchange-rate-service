using System.Text.Json;
using ExchangeRateService.Common;
using ExchangeRateService.DTOs.Treasury;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Services
{
    public class TreasuryExchangeRateService : ITreasuryExchangeRateService
    {
        private static readonly Dictionary<string, string> CurrencyMappings = new()
        {
            { "EUR", "Euro Zone-Euro" },
            { "GBP", "United Kingdom-Pound Sterling" },
            { "JPY", "Japan-Yen" },
            { "CAD", "Canada-Dollar" },
            { "AUD", "Australia-Dollar" },
        };

        private readonly HttpClient _httpClient;

        public TreasuryExchangeRateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Result<decimal>> GetExchangeRateAsync(
            string targetCurrency,
            DateTime transactionDate
        )
        {
            if (
                !CurrencyMappings.TryGetValue(
                    targetCurrency.ToUpper(System.Globalization.CultureInfo.CurrentCulture),
                    out string? treasuryCurrency
                )
            )
            {
                return Result<decimal>.Failure(ErrorCodes.UnsupportedCurrency);
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
                JsonSerializer.Deserialize<TreasuryExchangeRateApiResponse>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

            if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
            {
                return Result<decimal>.Failure(ErrorCodes.ExchangeRateApiEmptyResponse);
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
                return Result<decimal>.Failure(ErrorCodes.ExchangeRateNotFound);
            }

            if (!decimal.TryParse(bestMatch.ExchangeRate, out var rate))
            {
                return Result<decimal>.Failure(ErrorCodes.ExchangeRateParseError);
            }

            return Result<decimal>.Success(rate);
        }
    }
}
