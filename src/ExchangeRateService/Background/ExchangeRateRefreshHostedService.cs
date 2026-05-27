using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Background
{
    public class ExchangeRateRefreshHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ExchangeRateRefreshHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();

            var orchestrator =
                scope.ServiceProvider.GetRequiredService<IExchangeRateRefreshOrchestrator>();

            await orchestrator.EnsureBootstrapAsync();

            while (!stoppingToken.IsCancellationRequested)
            {
                await orchestrator.RefreshRecentAsync();

                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }
    }
}
