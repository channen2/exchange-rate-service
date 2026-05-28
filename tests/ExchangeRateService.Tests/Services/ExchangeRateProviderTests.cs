using ExchangeRateService.Background.Interfaces;
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
    public class ExchangeRateProviderTests : IDisposable
    {
        private const string CadCurrency = "CAD";
        private const string TreasuryCadCurrency = "Canada-Dollar";
        private readonly AppDbContext _db;
        private readonly ITreasuryExchangeRateApiClient _api;
        private readonly IExchangeRateIngestionBuffer _buffer;
        private readonly IMemoryCache _cache;
        private readonly ILogger<ExchangeRateProvider> _logger;

        private readonly ExchangeRateProvider _sut;

        public ExchangeRateProviderTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);

            _api = Substitute.For<ITreasuryExchangeRateApiClient>();
            _buffer = Substitute.For<IExchangeRateIngestionBuffer>();
            _cache = Substitute.For<IMemoryCache>();
            _logger = Substitute.For<ILogger<ExchangeRateProvider>>();

            _sut = new ExchangeRateProvider(_db, _api, _buffer, _cache, _logger);
        }

        [Fact]
        public async Task GetRateAsync_ShouldReturnCachedValue_WhenCacheHit()
        {
            // Arrange
            _cache
                .TryGetValue(Arg.Any<string>(), out Arg.Any<object>())
                .Returns(x =>
                {
                    x[1] = 1.5m;
                    return true;
                });

            // Act
            var result = await _sut.GetRateAsync(TreasuryCadCurrency, new DateTime(2026, 1, 1));

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1.5m, result.Value);

            await _api.DidNotReceiveWithAnyArgs().GetExchangeRatesAsync(default, default, default);
        }

        [Fact]
        public async Task GetRateAsync_ShouldReturnDbValue_WhenDbHitExists()
        {
            // Arrange

            var transactionDate = new DateTime(2026, 1, 10);

            _cache.TryGetValue(TreasuryCadCurrency, out Arg.Any<object>()).Returns(false);

            _db.ExchangeRates.Add(
                new ExchangeRate
                {
                    TreasuryCurrency = TreasuryCadCurrency,
                    CurrencyCode = CadCurrency,
                    EffectiveDate = transactionDate,
                    RecordDate = transactionDate,
                    Rate = 1.2m,
                }
            );

            await _db.SaveChangesAsync();

            // Act
            var result = await _sut.GetRateAsync(TreasuryCadCurrency, transactionDate);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(1.2m, result.Value);
        }

        [Fact]
        public async Task GetRateAsync_ShouldReturnApiValue_WhenDbMiss()
        {
            // Arrange
            var transactionDate = new DateTime(2026, 1, 10);

            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);

            _api.GetExchangeRatesAsync(
                    Arg.Any<DateTime>(),
                    Arg.Any<DateTime>(),
                    TreasuryCadCurrency
                )
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
                                    EffectiveDate = transactionDate.ToString("yyyy-MM-dd"),
                                    RecordDate = transactionDate.ToString("yyyy-MM-dd"),
                                },
                            ],
                        }
                    )
                );

            // Act
            var result = await _sut.GetRateAsync(TreasuryCadCurrency, transactionDate);

            // Assert
            await _buffer.Received(1).EnqueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());

            Assert.True(result.IsSuccess);
            Assert.Equal(1.25m, result.Value);
        }

        [Fact]
        public async Task GetRateAsync_ShouldReturnFailure_WhenApiReturnsNull()
        {
            // Arrange
            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);

            _api.GetExchangeRatesAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<string>())
                .Returns(
                    Result<TreasuryExchangeRateApiResponse>.Failure(
                        ErrorRegistry.ExchangeRateNotFound
                    )
                );

            // Act
            var result = await _sut.GetRateAsync(TreasuryCadCurrency, new DateTime(2026, 1, 1));

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorRegistry.ExchangeRateNotFound.Code, result.Error!.Code);
        }

        [Fact]
        public async Task GetRateAsync_ShouldReturnParseError_WhenExchangeRateInvalid()
        {
            // Arrange
            var transactionDate = new DateTime(2026, 1, 10);

            _cache.TryGetValue(Arg.Any<object>(), out Arg.Any<object>()).Returns(false);

            _api.GetExchangeRatesAsync(
                    Arg.Any<DateTime>(),
                    Arg.Any<DateTime>(),
                    TreasuryCadCurrency
                )
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
                                    EffectiveDate = "2026-01-10",
                                    RecordDate = "2026-01-10",
                                },
                            ],
                        }
                    )
                );

            // Act
            var result = await _sut.GetRateAsync(TreasuryCadCurrency, transactionDate);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(ErrorRegistry.ExchangeRateParseError.Code, result.Error!.Code);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}
