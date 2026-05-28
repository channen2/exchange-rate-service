using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Data;
using ExchangeRateService.DTOs.Treasury;
using ExchangeRateService.Models;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace ExchangeRateService.Tests.Services
{
    public class ExchangeRateIngestionServiceTests : IDisposable
    {
        private const string CadCurrency = "CAD";
        private const string TreasuryCadCurrency = "Canada-Dollar";

        private readonly AppDbContext _db;
        private readonly ITreasuryExchangeRateApiClient _apiClient;
        private readonly IMemoryCache _cache;
        private readonly ITreasuryCurrencyMapper _currencyMapper;
        private readonly ILogger<ExchangeRateIngestionService> _logger;

        private readonly ExchangeRateIngestionService _sut;

        public ExchangeRateIngestionServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);

            _apiClient = Substitute.For<ITreasuryExchangeRateApiClient>();
            _cache = Substitute.For<IMemoryCache>();
            _currencyMapper = Substitute.For<ITreasuryCurrencyMapper>();
            _logger = Substitute.For<ILogger<ExchangeRateIngestionService>>();

            _sut = new ExchangeRateIngestionService(
                _db,
                _apiClient,
                _cache,
                _currencyMapper,
                _logger
            );
        }

        [Fact]
        public async Task IngestRatesAsync_ShouldPersistNewExchangeRates_WhenApiSucceeds()
        {
            // Arrange
            var fromDate = new DateTime(2026, 1, 1);
            var toDate = new DateTime(2026, 1, 31);

            _currencyMapper
                .TryToInternal(TreasuryCadCurrency, out Arg.Any<string>())
                .Returns(x =>
                {
                    x[1] = CadCurrency;
                    return true;
                });

            _apiClient
                .GetExchangeRatesAsync(fromDate, toDate, null)
                .Returns(
                    Result<TreasuryExchangeRateApiResponse>.Success(
                        new TreasuryExchangeRateApiResponse
                        {
                            Data =
                            [
                                new()
                                {
                                    CountryCurrencyDescription = TreasuryCadCurrency,
                                    ExchangeRate = "1.25",
                                    EffectiveDate = "2026-01-01",
                                    RecordDate = "2026-01-01",
                                },
                            ],
                        }
                    )
                );

            // Act
            await _sut.IngestRatesAsync(fromDate, toDate);

            // Assert
            Assert.Single(_db.ExchangeRates);

            var entity = _db.ExchangeRates.First();

            Assert.Equal(CadCurrency, entity.CurrencyCode);
            Assert.Equal(TreasuryCadCurrency, entity.TreasuryCurrency);
            Assert.Equal(1.25m, entity.Rate);

            Assert.Single(_db.IngestionRuns);
            Assert.True(_db.IngestionRuns.First().Success);
        }

        [Fact]
        public async Task IngestRatesAsync_ShouldSkipExistingExchangeRates()
        {
            // Arrange
            var effectiveDate = new DateTime(2026, 1, 1);

            _db.ExchangeRates.Add(
                new ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    TreasuryCurrency = TreasuryCadCurrency,
                    CurrencyCode = CadCurrency,
                    Rate = 1.25m,
                    EffectiveDate = effectiveDate,
                    RecordDate = effectiveDate,
                    RetrievedAtUtc = DateTime.UtcNow,
                }
            );

            await _db.SaveChangesAsync();

            _currencyMapper
                .TryToInternal(TreasuryCadCurrency, out Arg.Any<string>())
                .Returns(x =>
                {
                    x[1] = CadCurrency;
                    return true;
                });

            _apiClient
                .GetExchangeRatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), null)
                .Returns(
                    Result<TreasuryExchangeRateApiResponse>.Success(
                        new TreasuryExchangeRateApiResponse
                        {
                            Data =
                            [
                                new()
                                {
                                    CountryCurrencyDescription = TreasuryCadCurrency,
                                    ExchangeRate = "1.25",
                                    EffectiveDate = "2026-01-01",
                                    RecordDate = "2026-01-01",
                                },
                            ],
                        }
                    )
                );

            // Act
            await _sut.IngestRatesAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            // Assert
            Assert.Single(_db.ExchangeRates);
        }

        [Fact]
        public async Task IngestRatesAsync_ShouldSkipInvalidRecords()
        {
            // Arrange
            _apiClient
                .GetExchangeRatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), null)
                .Returns(
                    Result<TreasuryExchangeRateApiResponse>.Success(
                        new TreasuryExchangeRateApiResponse
                        {
                            Data =
                            [
                                new()
                                {
                                    CountryCurrencyDescription = TreasuryCadCurrency,
                                    ExchangeRate = "INVALID",
                                    EffectiveDate = "2026-01-01",
                                    RecordDate = "2026-01-01",
                                },
                            ],
                        }
                    )
                );

            // Act
            await _sut.IngestRatesAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            // Assert
            Assert.Empty(_db.ExchangeRates);

            Assert.Single(_db.IngestionRuns);
            Assert.True(_db.IngestionRuns.First().Success);
        }

        [Fact]
        public async Task IngestRatesAsync_ShouldPersistFailedRun_WhenApiFails()
        {
            // Arrange
            _apiClient
                .GetExchangeRatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), null)
                .Returns(
                    Result<TreasuryExchangeRateApiResponse>.Failure(
                        ErrorRegistry.ExternalServiceError
                    )
                );

            // Act
            await _sut.IngestRatesAsync(new DateTime(2026, 1, 1), new DateTime(2026, 1, 31));

            // Assert
            Assert.Empty(_db.ExchangeRates);
            Assert.Single(_db.IngestionRuns);

            var run = _db.IngestionRuns.First();

            Assert.False(run.Success);
            Assert.NotNull(run.ErrorMessage);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
