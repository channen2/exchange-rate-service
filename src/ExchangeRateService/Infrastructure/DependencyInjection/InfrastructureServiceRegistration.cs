using ExchangeRateService.Configuration;
using ExchangeRateService.Data;
using ExchangeRateService.Infrastructure.Http;
using ExchangeRateService.Integrations.Treasury;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class InfrastructureServiceRegistration
    {
        public static IServiceCollection AddInfrastructure(
            this IServiceCollection services,
            IConfiguration config,
            IHostEnvironment env
        )
        {
            services
                .AddHttpClient<ITreasuryExchangeRateApiClient, TreasuryExchangeRateApiClient>()
                .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
                .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());

            services.AddDbContext<AppDbContext>(options =>
            {
                if (env.IsEnvironment("Testing"))
                {
                    options.UseInMemoryDatabase("TestDb");
                }
                else
                {
                    options.UseSqlServer(
                        config.GetConnectionString("DefaultConnection"),
                        sql =>
                        {
                            sql.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(10),
                                errorNumbersToAdd: null
                            );
                        }
                    );
                }
            });

            services.AddMemoryCache();

            services.AddSingleton<ITreasuryCurrencyMapper, TreasuryCurrencyMapper>();

            services.Configure<TreasuryCurrencyOptions>(
                config.GetSection("TreasuryCurrencyOptions")
            );

            return services;
        }
    }
}
