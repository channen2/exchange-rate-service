using ExchangeRateService.Background;
using ExchangeRateService.Background.Interfaces;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class BackgroundServiceRegistration
    {
        public static IServiceCollection AddBackgroundWorkers(
            this IServiceCollection services,
            IHostEnvironment env
        )
        {
            services.AddSingleton<IExchangeRateIngestionBuffer, ExchangeRateIngestionBuffer>();

            services.AddHostedService<ExchangeRateIngestionWorker>();

            if (!env.IsEnvironment("Testing"))
            {
                services.AddHostedService<ExchangeRateRefreshHostedService>();
            }

            return services;
        }
    }
}
