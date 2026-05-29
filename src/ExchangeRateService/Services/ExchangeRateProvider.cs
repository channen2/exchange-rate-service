using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Data;
using ExchangeRateService.Integrations.Treasury;
using ExchangeRateService.Integrations.Treasury.DTOs;
using ExchangeRateService.Logging;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeRateService.Services
{
    public class ExchangeRateProvider(
        AppDbContext db,
        ITreasuryExchangeRateApiClient treasuryService,
        IExchangeRateIngestionBuffer ingestionBuffer,
        IMemoryCache cache,
        ITreasuryCurrencyMapper treasuryCurrencyMapper,
        ILogger<ExchangeRateProvider> logger
    ) : IExchangeRateProvider
    {
        private readonly AppDbContext _db = db;
        private readonly ITreasuryExchangeRateApiClient _treasuryApiClient = treasuryService;
        private readonly IExchangeRateIngestionBuffer _ingestionBuffer = ingestionBuffer;

        private readonly IMemoryCache _cache = cache;

        private readonly ILogger<ExchangeRateProvider> _logger = logger;

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
            var exchangeRateFromDb = await TryGetFromDbAsync(
                treasuryCurrency,
                transactionDate,
                cutoffDate
            );

            if (exchangeRateFromDb is not null)
            {
                LogMessages.DbHit(_logger, treasuryCurrency, transactionDate);
                TryCache(
                    exchangeRateFromDb.Rate,
                    exchangeRateFromDb.RecordDate,
                    expectedRecordDate,
                    cacheKey
                );
                return Result<decimal>.Success(exchangeRateFromDb.Rate);
            }

            LogMessages.DbMiss(_logger, treasuryCurrency, transactionDate);

            // Finally try API layer
            var exchangeRateFromApi = await TryGetFromApiAsync(
                treasuryCurrency,
                transactionDate,
                cutoffDate
            );

            LogMessages.TreasuryApiFallback(_logger, treasuryCurrency, transactionDate, cutoffDate);

            var currencyCode = treasuryCurrencyMapper.ToInternal(treasuryCurrency);

            if (exchangeRateFromApi is null)
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
                        ["currency"] = currencyCode,
                        ["transactionDate"] = DateFormats.IsoDate(transactionDate),
                    }
                );
            }

            if (!decimal.TryParse(exchangeRateFromApi.ExchangeRate, out var rate))
            {
                LogMessages.TreasuryApiParseFailure(
                    _logger,
                    treasuryCurrency,
                    exchangeRateFromApi.ExchangeRate
                );

                return Result<decimal>.Failure(
                    ErrorRegistry.ExchangeRateParseError,
                    new Dictionary<string, object>
                    {
                        ["currency"] = currencyCode,
                        ["transactionDate"] = DateFormats.IsoDate(transactionDate),
                        ["exchangeRateValue"] = exchangeRateFromApi.ExchangeRate,
                    }
                );
            }

            TryCache(
                rate,
                DateFormats.ParseIsoDate(exchangeRateFromApi.RecordDate),
                expectedRecordDate,
                cacheKey
            );

            // Enqueue ingestion to backfill DB for future requests
            _ingestionBuffer.EnqueueAsync(cutoffDate, transactionDate);

            return Result<decimal>.Success(rate);
        }

        private async Task<ExchangeRate?> TryGetFromDbAsync(
            string treasuryCurrency,
            DateTime transactionDate,
            DateTime cutoffDate
        )
        {
            return await _db
                .ExchangeRates.Where(x =>
                    x.TreasuryCurrency == treasuryCurrency
                    && x.EffectiveDate <= transactionDate
                    && x.EffectiveDate >= cutoffDate
                )
                .OrderByDescending(x => x.EffectiveDate)
                .FirstOrDefaultAsync();
        }

        private async Task<TreasuryExchangeRateRecord?> TryGetFromApiAsync(
            string treasuryCurrency,
            DateTime transactionDate,
            DateTime cutoffDate
        )
        {
            var apiResult = await _treasuryApiClient.GetExchangeRatesAsync(
                cutoffDate,
                transactionDate,
                treasuryCurrency
            );

            if (!apiResult.IsSuccess || apiResult.Value?.Data is null)
            {
                return null;
            }

            return ResolveFromApi(apiResult.Value, treasuryCurrency, transactionDate, cutoffDate);
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
                    EffectiveDate = DateFormats.ParseIsoDate(x.EffectiveDate),
                    RecordDate = DateFormats.ParseIsoDate(x.RecordDate),
                })
                .Where(x => x.EffectiveDate <= transactionDate)
                .Where(x => x.EffectiveDate >= cutoffDate)
                .OrderByDescending(x => x.EffectiveDate)
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
