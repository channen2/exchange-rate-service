using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Common;
using ExchangeRateService.Data;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Services
{
    public class ExchangeRateRefreshOrchestrator(
        AppDbContext db,
        IExchangeRateIngestionBuffer buffer
    ) : IExchangeRateRefreshOrchestrator
    {
        private readonly AppDbContext _db = db;

        private readonly IExchangeRateIngestionBuffer _buffer = buffer;

        private const int BootstrapYears = 5;

        public async Task EnsureBootstrapAsync()
        {
            var hasAnyRates = await _db.ExchangeRates.AnyAsync();

            if (hasAnyRates)
            {
                return;
            }

            await BootstrapHistoricalAsync();
        }

        public async Task RefreshRecentAsync()
        {
            var now = DateTime.UtcNow.Date;

            var currentQuarter = TreasuryDateHelper.GetQuarterWindow(now);
            var previousQuarter = TreasuryDateHelper.GetQuarterWindow(now.AddMonths(-3));

            await _buffer.EnqueueAsync(currentQuarter.from, currentQuarter.to);
            await _buffer.EnqueueAsync(previousQuarter.from, previousQuarter.to);
        }

        private async Task BootstrapHistoricalAsync()
        {
            var end = DateTime.UtcNow.Date;
            var start = end.AddYears(-BootstrapYears);

            foreach (var (from, to) in TreasuryDateHelper.GetQuarterWindows(start, end))
            {
                await _buffer.EnqueueAsync(from, to);
            }
        }
    }
}
