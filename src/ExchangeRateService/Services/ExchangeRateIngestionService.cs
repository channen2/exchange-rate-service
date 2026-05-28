using ExchangeRateService.Common;
using ExchangeRateService.Data;
using ExchangeRateService.Logging;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeRateService.Services
{
    public class ExchangeRateIngestionService : IExchangeRateIngestionService
    {
        private readonly AppDbContext _db;

        private readonly ITreasuryExchangeRateApiClient _treasuryApiClient;

        private readonly IMemoryCache _cache;

        private readonly ITreasuryCurrencyMapper _treasuryCurrencyMapper;

        private readonly ILogger<ExchangeRateIngestionService> _logger;

        public ExchangeRateIngestionService(
            AppDbContext db,
            ITreasuryExchangeRateApiClient treasuryService,
            IMemoryCache cache,
            ITreasuryCurrencyMapper treasuryCurrencyMapper,
            ILogger<ExchangeRateIngestionService> logger
        )
        {
            _db = db;
            _treasuryApiClient = treasuryService;
            _cache = cache;
            _treasuryCurrencyMapper = treasuryCurrencyMapper;
            _logger = logger;
        }

        public async Task IngestRatesAsync(DateTime fromDate, DateTime toDate)
        {
            var run = new IngestionRun
            {
                Id = Guid.NewGuid(),
                FromDateUtc = fromDate,
                ToDateUtc = toDate,
                StartedAtUtc = DateTime.UtcNow,
            };

            LogMessages.IngestionStarted(_logger, fromDate, toDate);

            try
            {
                var apiResult = await _treasuryApiClient.GetExchangeRatesAsync(
                    fromDate,
                    toDate,
                    null
                );

                if (!apiResult.IsSuccess || apiResult.Value?.Data is null)
                {
                    run.Success = false;
                    run.ErrorMessage = apiResult.Error?.Message;

                    _db.IngestionRuns.Add(run);
                    await _db.SaveChangesAsync();
                    return;
                }

                var records = apiResult.Value.Data;

                var existing = await _db
                    .ExchangeRates.Where(x =>
                        x.EffectiveDate >= fromDate && x.EffectiveDate <= toDate
                    )
                    .Select(x => new
                    {
                        x.TreasuryCurrency,
                        x.EffectiveDate,
                        x.RecordDate,
                    })
                    .ToListAsync();

                var existingSet = existing
                    .Select(x => (x.TreasuryCurrency, x.EffectiveDate, x.RecordDate))
                    .ToHashSet();

                var now = DateTime.UtcNow;

                var newEntities = new List<ExchangeRate>();

                foreach (var record in records)
                {
                    var treasuryCurrency = record.CountryCurrencyDescription;

                    if (
                        !DateTime.TryParse(record.RecordDate, out var recordDate)
                        || !DateTime.TryParse(record.EffectiveDate, out var effectiveDate)
                        || !decimal.TryParse(record.ExchangeRate, out var rate)
                        || !_treasuryCurrencyMapper.TryToInternal(
                            treasuryCurrency,
                            out var currencyCode
                        )
                    )
                    {
                        LogMessages.IngestionRecordSkipped(
                            _logger,
                            record.CountryCurrencyDescription,
                            record.EffectiveDate ?? "null",
                            record.RecordDate ?? "null",
                            record.ExchangeRate ?? "null"
                        );
                        continue;
                    }

                    var key = (treasuryCurrency, effectiveDate, recordDate);

                    if (existingSet.Contains(key))
                    {
                        continue;
                    }

                    newEntities.Add(
                        new ExchangeRate
                        {
                            Id = Guid.NewGuid(),
                            TreasuryCurrency = treasuryCurrency,
                            CurrencyCode = currencyCode,
                            Rate = rate,
                            EffectiveDate = effectiveDate,
                            RecordDate = recordDate,
                            RetrievedAtUtc = now,
                        }
                    );
                }

                if (newEntities.Count > 0)
                {
                    _db.ExchangeRates.AddRange(newEntities);
                }

                run.Success = true;

                _db.IngestionRuns.Add(run);
                await _db.SaveChangesAsync();

                LogMessages.IngestionCompleted(_logger, fromDate, toDate, newEntities.Count);

                if (newEntities.Count > 0)
                {
                    foreach (var entity in newEntities)
                    {
                        var cacheKey = ExchangeRateCacheKey.FromRecordDate(
                            entity.TreasuryCurrency,
                            entity.RecordDate
                        );
                        _cache.Remove(cacheKey);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessages.IngestionFailed(_logger, fromDate, toDate, ex);
                run.Success = false;
                run.ErrorMessage = ex.Message;

                _db.IngestionRuns.Add(run);
                await _db.SaveChangesAsync();
            }
        }
    }
}
