using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Data;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Services
{
    public class ExchangeRateRefreshOrchestrator : IExchangeRateRefreshOrchestrator
    {
        private readonly AppDbContext _db;

        private readonly IExchangeRateIngestionBuffer _buffer;

        private const int BootstrapYears = 5;

        public ExchangeRateRefreshOrchestrator(AppDbContext db, IExchangeRateIngestionBuffer buffer)
        {
            _db = db;
            _buffer = buffer;
        }

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

            var currentQuarter = GetQuarterWindow(now);
            var previousQuarter = GetQuarterWindow(now.AddMonths(-3));

            await _buffer.EnqueueAsync(currentQuarter.from, currentQuarter.to);
            await _buffer.EnqueueAsync(previousQuarter.from, previousQuarter.to);
        }

        private async Task BootstrapHistoricalAsync()
        {
            var end = DateTime.UtcNow.Date;
            var start = end.AddYears(-BootstrapYears);

            foreach (var (from, to) in GetQuarterWindows(start, end))
            {
                await _buffer.EnqueueAsync(from, to);
            }
        }

        private static IEnumerable<(DateTime from, DateTime to)> GetQuarterWindows(
            DateTime start,
            DateTime end
        )
        {
            var iterationDate = new DateTime(start.Year, start.Month, 1);

            while (iterationDate <= end)
            {
                var window = GetQuarterWindow(iterationDate);

                yield return window;

                iterationDate = window.to.AddDays(1);
            }
        }

        private static (DateTime from, DateTime to) GetQuarterWindow(DateTime date)
        {
            var quarter = ((date.Month - 1) / 3) + 1;
            var startMonth = ((quarter - 1) * 3) + 1;

            var from = new DateTime(date.Year, startMonth, 1);
            var to = from.AddMonths(3).AddDays(-1);

            return (from, to);
        }
    }
}
