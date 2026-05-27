using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Data;
using ExchangeRateService.DTOs.Treasury;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeRateService.Services
{
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        private readonly AppDbContext _db;
        private readonly ITreasuryExchangeRateService _treasuryService;
        private readonly IExchangeRateIngestionBuffer _ingestionBuffer;

        private readonly IMemoryCache _cache;

        public ExchangeRateProvider(
            AppDbContext db,
            ITreasuryExchangeRateService treasuryService,
            IExchangeRateIngestionBuffer ingestionBuffer,
            IMemoryCache cache
        )
        {
            _db = db;
            _treasuryService = treasuryService;
            _ingestionBuffer = ingestionBuffer;
            _cache = cache;
        }

        public async Task<Result<decimal>> GetRateAsync(
            string treasuryCurrency,
            DateTime transactionDate
        )
        {
            var expectedRecordDate = TreasuryDateHelper.GetTreasuryRecordDate(transactionDate);

            var cacheKey = ExchangeRateCacheKey.FromTransactionDate(
                treasuryCurrency,
                transactionDate
            );

            // Try cache layer first
            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                return Result<decimal>.Success(cachedRate);
            }

            // Fallback to DB layer
            var db = await TryGetFromDbAsync(treasuryCurrency, transactionDate);

            if (db is not null)
            {
                TryCache(db.Rate, db.RecordDate, expectedRecordDate, cacheKey);
                return Result<decimal>.Success(db.Rate);
            }

            // Finally try API layer
            var api = await TryGetFromApiAsync(treasuryCurrency, transactionDate);

            if (api is null)
            {
                return Result<decimal>.Failure(
                    ErrorRegistry.ExchangeRateNotFound,
                    new Dictionary<string, object>
                    {
                        ["currency"] = treasuryCurrency,
                        ["transactionDate"] = transactionDate.ToString("yyyy-MM-dd"),
                    }
                );
            }

            if (!decimal.TryParse(api.ExchangeRate, out var rate))
            {
                return Result<decimal>.Failure(
                    ErrorRegistry.ExchangeRateParseError,
                    new Dictionary<string, object> { ["exchangeRateValue"] = api.ExchangeRate }
                );
            }

            TryCache(rate, DateTime.Parse(api.RecordDate), expectedRecordDate, cacheKey);

            // Enqueue ingestion to backfill DB for future requests
            _ingestionBuffer.EnqueueAsync(transactionDate.AddMonths(-6), transactionDate);

            return Result<decimal>.Success(rate);
        }

        private async Task<ExchangeRate?> TryGetFromDbAsync(
            string currency,
            DateTime transactionDate
        )
        {
            var cutoff = transactionDate.AddMonths(-6);

            return await _db
                .ExchangeRates.Where(x =>
                    x.CurrencyCode == currency
                    && x.EffectiveDate <= transactionDate
                    && x.EffectiveDate >= cutoff
                )
                .OrderByDescending(x => x.EffectiveDate)
                .ThenByDescending(x => x.RecordDate)
                .FirstOrDefaultAsync();
        }

        private async Task<TreasuryExchangeRateRecord?> TryGetFromApiAsync(
            string currency,
            DateTime transactionDate
        )
        {
            var apiResult = await _treasuryService.GetExchangeRatesAsync(
                transactionDate.AddMonths(-6),
                transactionDate,
                currency
            );

            if (!apiResult.IsSuccess || apiResult.Value?.Data is null)
            {
                return null;
            }

            return ResolveFromApi(apiResult.Value, currency, transactionDate);
        }

        private static TreasuryExchangeRateRecord? ResolveFromApi(
            TreasuryExchangeRateApiResponse response,
            string treasuryCurrency,
            DateTime transactionDate
        )
        {
            var cutoff = transactionDate.AddMonths(-6);

            return response
                .Data.Where(x => x.CountryCurrencyDescription == treasuryCurrency)
                .Select(x => new
                {
                    Record = x,
                    EffectiveDate = DateTime.Parse(x.EffectiveDate),
                    RecordDate = DateTime.Parse(x.RecordDate),
                })
                .Where(x => x.EffectiveDate <= transactionDate)
                .Where(x => x.EffectiveDate >= cutoff)
                .OrderByDescending(x => x.EffectiveDate)
                .ThenByDescending(x => x.RecordDate)
                .Select(x => x.Record)
                .FirstOrDefault();
        }

        private void TryCache(
            decimal rate,
            DateTime recordDate,
            DateTime expectedRecordDate,
            string cacheKey
        )
        {
            if (recordDate != expectedRecordDate)
            {
                return;
            }

            _cache.Set(cacheKey, rate, TimeSpan.FromHours(12));
        }
    }
}
