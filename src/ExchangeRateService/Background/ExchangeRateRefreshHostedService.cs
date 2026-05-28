using ExchangeRateService.Logging;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Background
{
    public class ExchangeRateRefreshHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ExchangeRateRefreshHostedService> logger
    ) : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

        private readonly ILogger<ExchangeRateRefreshHostedService> _logger = logger;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var orchestrator =
                scope.ServiceProvider.GetRequiredService<IExchangeRateRefreshOrchestrator>();

            await orchestrator.EnsureBootstrapAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await orchestrator.RefreshRecentAsync();

                LogMessages.RefreshJobExecuted(_logger, DateTime.UtcNow);

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
