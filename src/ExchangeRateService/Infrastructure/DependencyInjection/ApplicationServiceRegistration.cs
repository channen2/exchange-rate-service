using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class ApplicationServiceRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<ITransactionService, TransactionService>();
            services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
            services.AddScoped<IExchangeRateProvider, ExchangeRateProvider>();
            services.AddScoped<IExchangeRateIngestionService, ExchangeRateIngestionService>();
            services.AddScoped<IExchangeRateRefreshOrchestrator, ExchangeRateRefreshOrchestrator>();

            return services;
        }
    }
}
