using System.Text.Json;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Integrations.Treasury.DTOs;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Services
{
    public class TreasuryExchangeRateApiClient(HttpClient httpClient)
        : ITreasuryExchangeRateApiClient
    {
        private readonly HttpClient _httpClient = httpClient;

        private const int PageSize = 1000;

        private const int MaxPages = 1000;

        private const string BaseUrl =
            "https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange";

        private const string Sort = "record_date,country_currency_desc";

        private const string Fields =
            "country_currency_desc,exchange_rate,record_date,effective_date";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        public async Task<Result<TreasuryExchangeRateApiResponse>> GetExchangeRatesAsync(
            DateTime fromDate,
            DateTime toDate,
            string? treasuryCurrency = null
        )
        {
            var allRecords = new List<TreasuryExchangeRateRecord>();

            var filters = new List<string>
            {
                $"record_date:gte:{fromDate:yyyy-MM-dd}",
                $"record_date:lte:{toDate:yyyy-MM-dd}",
            };

            if (!string.IsNullOrWhiteSpace(treasuryCurrency))
            {
                filters.Add($"country_currency_desc:eq:{treasuryCurrency}");
            }

            var filterString = string.Join(",", filters);

            var pageNumber = 1;

            while (true)
            {
                var url =
                    BaseUrl
                    + $"?filter={filterString}"
                    + $"&sort={Sort}"
                    + $"&page[number]={pageNumber}"
                    + $"&page[size]={PageSize}"
                    + $"&fields={Fields}"
                    + "&format=json";

                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return Result<TreasuryExchangeRateApiResponse>.Failure(
                        ErrorRegistry.ExternalServiceError,
                        new Dictionary<string, object>
                        {
                            ["statusCode"] = response.StatusCode.ToString(),
                            ["fromDate"] = DateFormats.IsoDate(fromDate),
                            ["toDate"] = DateFormats.IsoDate(toDate),
                            ["treasuryCurrency"] = treasuryCurrency ?? "ALL",
                        }
                    );
                }

                var json = await response.Content.ReadAsStringAsync();

                var page = JsonSerializer.Deserialize<TreasuryExchangeRateApiResponse>(
                    json,
                    JsonOptions
                );

                if (page?.Data == null)
                {
                    return Result<TreasuryExchangeRateApiResponse>.Failure(
                        ErrorRegistry.ExchangeRateNotFound,
                        new Dictionary<string, object>
                        {
                            ["fromDate"] = DateFormats.IsoDate(fromDate),
                            ["toDate"] = DateFormats.IsoDate(toDate),
                            ["treasuryCurrency"] = treasuryCurrency ?? "ALL",
                        }
                    );
                }

                allRecords.AddRange(page.Data);

                if (page.Data.Count < PageSize)
                {
                    break;
                }

                pageNumber++;

                if (pageNumber > MaxPages)
                {
                    return Result<TreasuryExchangeRateApiResponse>.Failure(
                        ErrorRegistry.TreasuryPaginationLimitExceeded,
                        new Dictionary<string, object>
                        {
                            ["fromDate"] = DateFormats.IsoDate(fromDate),
                            ["toDate"] = DateFormats.IsoDate(toDate),
                            ["treasuryCurrency"] = treasuryCurrency ?? "ALL",
                            ["pageSize"] = PageSize,
                            ["maxPages"] = MaxPages,
                            ["lastPageReached"] = pageNumber,
                        }
                    );
                }
            }

            Console.WriteLine("WOOHOOO, got this many things-, {0}", allRecords.Count);
            return Result<TreasuryExchangeRateApiResponse>.Success(
                new TreasuryExchangeRateApiResponse { Data = allRecords }
            );
        }
    }
}
