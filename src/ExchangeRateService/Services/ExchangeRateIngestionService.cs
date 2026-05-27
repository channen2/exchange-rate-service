using ExchangeRateService.Common;
using ExchangeRateService.Data;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExchangeRateService.Services
{
    public class ExchangeRateIngestionService : IExchangeRateIngestionService
    {
        private readonly AppDbContext _db;
        private readonly ITreasuryExchangeRateService _treasuryService;
        private readonly IMemoryCache _cache;

        public ExchangeRateIngestionService(
            AppDbContext db,
            ITreasuryExchangeRateService treasuryService,
            IMemoryCache cache
        )
        {
            _db = db;
            _treasuryService = treasuryService;
            _cache = cache;
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

            try
            {
                var apiResult = await _treasuryService.GetExchangeRatesAsync(
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
                        x.CurrencyCode,
                        x.EffectiveDate,
                        x.RecordDate,
                    })
                    .ToListAsync();

                var existingSet = existing
                    .Select(x => (x.CurrencyCode, x.EffectiveDate, x.RecordDate))
                    .ToHashSet();

                var now = DateTime.UtcNow;

                var newEntities = new List<ExchangeRate>();

                foreach (var record in records)
                {
                    if (
                        !DateTime.TryParse(record.RecordDate, out var recordDate)
                        || !DateTime.TryParse(record.EffectiveDate, out var effectiveDate)
                        || !decimal.TryParse(record.ExchangeRate, out var rate)
                    )
                    {
                        continue;
                    }

                    var currency = record.CountryCurrencyDescription.ToUpperInvariant();

                    var key = (currency, effectiveDate, recordDate);

                    if (existingSet.Contains(key))
                    {
                        continue;
                    }

                    newEntities.Add(
                        new ExchangeRate
                        {
                            Id = Guid.NewGuid(),
                            CurrencyCode = currency,
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

                if (newEntities.Count > 0)
                {
                    foreach (var entity in newEntities)
                    {
                        var cacheKey = ExchangeRateCacheKey.FromRecordDate(
                            entity.CurrencyCode,
                            entity.RecordDate
                        );
                        _cache.Remove(cacheKey);
                    }
                }
            }
            catch (Exception ex)
            {
                run.Success = false;
                run.ErrorMessage = ex.Message;

                _db.IngestionRuns.Add(run);
                await _db.SaveChangesAsync();
            }
        }
    }
}
