using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Logging;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Background
{
    public class ExchangeRateIngestionWorker(
        IExchangeRateIngestionBuffer buffer,
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeRateIngestionWorker> logger
    ) : BackgroundService
    {
        private readonly IExchangeRateIngestionBuffer _buffer = buffer;
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
        private readonly ILogger<ExchangeRateIngestionWorker> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            LogMessages.IngestionWorkerStarted(_logger, DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                var (fromDate, toDate) = await _buffer.DequeueAsync(stoppingToken);

                try
                {
                    using var scope = _scopeFactory.CreateScope();

                    var ingestionService =
                        scope.ServiceProvider.GetRequiredService<IExchangeRateIngestionService>();

                    await ingestionService.IngestRatesAsync(fromDate, toDate);
                }
                catch (Exception ex)
                {
                    // swallow or log later (important: don't kill loop)
                }
            }
        }
    }
}
