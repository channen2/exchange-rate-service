using ExchangeRateService.Background;
using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Data;
using Microsoft.EntityFrameworkCore;
using NSubstitute;

namespace ExchangeRateService.Tests.Services
{
    public class ExchangeRateRefreshOrchestratorTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly IExchangeRateIngestionBuffer _buffer;
        private readonly ExchangeRateRefreshOrchestrator _sut;

        public ExchangeRateRefreshOrchestratorTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _db = new AppDbContext(options);
            _buffer = Substitute.For<IExchangeRateIngestionBuffer>();

            _sut = new ExchangeRateRefreshOrchestrator(_db, _buffer);
        }

        [Fact]
        public async Task EnsureBootstrapAsync_ShouldEnqueueHistoricalData_WhenDbIsEmpty()
        {
            // Arrange
            Assert.Empty(_db.ExchangeRates);

            // Act
            await _sut.EnsureBootstrapAsync();

            // Assert
            await _buffer.Received().EnqueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task EnsureBootstrapAsync_ShouldNotEnqueue_WhenDataExists()
        {
            // Arrange
            _db.ExchangeRates.Add(
                new Models.ExchangeRate
                {
                    Id = Guid.NewGuid(),
                    TreasuryCurrency = "Canada-Dollar",
                    CurrencyCode = "CAD",
                    Rate = 1.2m,
                    EffectiveDate = DateTime.UtcNow,
                    RecordDate = DateTime.UtcNow,
                    RetrievedAtUtc = DateTime.UtcNow,
                }
            );

            await _db.SaveChangesAsync();

            // Act
            await _sut.EnsureBootstrapAsync();

            // Assert
            await _buffer.DidNotReceive().EnqueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }

        [Fact]
        public async Task RefreshRecentAsync_ShouldEnqueuePreviousQuarter()
        {
            // Act
            await _sut.RefreshRecentAsync();

            // Assert
            await _buffer.Received(1).EnqueueAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>());
        }

        public void Dispose()
        {
            _db.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
