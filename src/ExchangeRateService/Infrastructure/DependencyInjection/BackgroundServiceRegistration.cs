using ExchangeRateService.Background;
using ExchangeRateService.Background.Interfaces;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class BackgroundServiceRegistration
    {
        public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
        {
            services.AddSingleton<IExchangeRateIngestionBuffer, ExchangeRateIngestionBuffer>();

            services.AddHostedService<ExchangeRateIngestionWorker>();
            services.AddHostedService<ExchangeRateRefreshHostedService>();

            return services;
        }
    }
}
