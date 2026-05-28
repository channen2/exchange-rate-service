using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Data;
using ExchangeRateService.DTOs.Treasury;
using ExchangeRateService.Logging;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeRateService.Services
{
    public class ExchangeRateProvider : IExchangeRateProvider
    {
        private readonly AppDbContext _db;
        private readonly ITreasuryExchangeRateApiClient _treasuryService;
        private readonly IExchangeRateIngestionBuffer _ingestionBuffer;

        private readonly IMemoryCache _cache;
        private readonly ILogger<ExchangeRateProvider> _logger;

        public ExchangeRateProvider(
            AppDbContext db,
            ITreasuryExchangeRateApiClient treasuryService,
            IExchangeRateIngestionBuffer ingestionBuffer,
            IMemoryCache cache,
            ILogger<ExchangeRateProvider> logger
        )
        {
            _db = db;
            _treasuryService = treasuryService;
            _ingestionBuffer = ingestionBuffer;
            _cache = cache;
            _logger = logger;
        }

        public async Task<Result<decimal>> GetRateAsync(
            string treasuryCurrency,
            DateTime transactionDate
        )
        {
            var cutoffDate = transactionDate.AddMonths(-6);

            var expectedRecordDate = TreasuryDateHelper.GetTreasuryRecordDate(transactionDate);

            var cacheKey = ExchangeRateCacheKey.FromTransactionDate(
                treasuryCurrency,
                transactionDate
            );

            // Try cache layer first
            if (_cache.TryGetValue(cacheKey, out decimal cachedRate))
            {
                LogMessages.CacheHit(_logger, treasuryCurrency, transactionDate, cacheKey);
                return Result<decimal>.Success(cachedRate);
            }

            LogMessages.CacheMiss(_logger, treasuryCurrency, transactionDate, cacheKey);

            // Fallback to DB layer
            var db = await TryGetFromDbAsync(treasuryCurrency, transactionDate, cutoffDate);

            if (db is not null)
            {
                LogMessages.DbHit(_logger, treasuryCurrency, transactionDate);
                TryCache(db.Rate, db.RecordDate, expectedRecordDate, cacheKey);
                return Result<decimal>.Success(db.Rate);
            }

            LogMessages.DbMiss(_logger, treasuryCurrency, transactionDate);

            // Finally try API layer
            var api = await TryGetFromApiAsync(treasuryCurrency, transactionDate, cutoffDate);

            LogMessages.TreasuryApiFallback(_logger, treasuryCurrency, transactionDate, cutoffDate);

            if (api is null)
            {
                LogMessages.TreasuryApiFailure(
                    _logger,
                    treasuryCurrency,
                    transactionDate,
                    cutoffDate
                );

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
                LogMessages.TreasuryApiParseFailure(_logger, treasuryCurrency, api.ExchangeRate);

                return Result<decimal>.Failure(
                    ErrorRegistry.ExchangeRateParseError,
                    new Dictionary<string, object> { ["exchangeRateValue"] = api.ExchangeRate }
                );
            }

            TryCache(rate, DateTime.Parse(api.RecordDate), expectedRecordDate, cacheKey);

            // Enqueue ingestion to backfill DB for future requests
            _ingestionBuffer.EnqueueAsync(cutoffDate, transactionDate);

            return Result<decimal>.Success(rate);
        }

        private async Task<ExchangeRate?> TryGetFromDbAsync(
            string currency,
            DateTime transactionDate,
            DateTime cutoffDate
        )
        {
            return await _db
                .ExchangeRates.Where(x =>
                    x.CurrencyCode == currency
                    && x.EffectiveDate <= transactionDate
                    && x.EffectiveDate >= cutoffDate
                )
                .OrderByDescending(x => x.EffectiveDate)
                .ThenByDescending(x => x.RecordDate)
                .FirstOrDefaultAsync();
        }

        private async Task<TreasuryExchangeRateRecord?> TryGetFromApiAsync(
            string currency,
            DateTime transactionDate,
            DateTime cutoffDate
        )
        {
            var apiResult = await _treasuryService.GetExchangeRatesAsync(
                cutoffDate,
                transactionDate,
                currency
            );

            if (!apiResult.IsSuccess || apiResult.Value?.Data is null)
            {
                return null;
            }

            return ResolveFromApi(apiResult.Value, currency, transactionDate, cutoffDate);
        }

        private static TreasuryExchangeRateRecord? ResolveFromApi(
            TreasuryExchangeRateApiResponse response,
            string treasuryCurrency,
            DateTime transactionDate,
            DateTime cutoffDate
        )
        {
            return response
                .Data.Where(x => x.CountryCurrencyDescription == treasuryCurrency)
                .Select(x => new
                {
                    Record = x,
                    EffectiveDate = DateTime.Parse(x.EffectiveDate),
                    RecordDate = DateTime.Parse(x.RecordDate),
                })
                .Where(x => x.EffectiveDate <= transactionDate)
                .Where(x => x.EffectiveDate >= cutoffDate)
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
