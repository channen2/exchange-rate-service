using Polly;
using Polly.Extensions.Http;

namespace ExchangeRateService.Infrastructure.Http
{
    public static class PollyPolicies
    {
        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var jitterer = new Random();

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3,
                    retry =>
                    {
                        var exponentialDelay = TimeSpan.FromMilliseconds(Math.Pow(2, retry) * 200);
                        var jitter = TimeSpan.FromMilliseconds(jitterer.Next(0, 100));
                        return exponentialDelay + jitter;
                    }
                );
        }

        public static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
        }
    }
}
