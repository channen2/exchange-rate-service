using ExchangeRateService.Configuration;
using ExchangeRateService.Data;
using ExchangeRateService.Infrastructure.Http;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            services
                .AddHttpClient<ITreasuryExchangeRateApiClient, TreasuryExchangeRateApiClient>()
                .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
                .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"))
            );

            services.AddMemoryCache();

            services.AddSingleton<ITreasuryCurrencyMapper, TreasuryCurrencyMapper>();

            services.Configure<TreasuryCurrencyOptions>(
                config.GetSection("TreasuryCurrencyOptions")
            );

            return services;
        }
    }
}
