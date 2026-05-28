using ExchangeRateService.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateService.Infrastructure.DependencyInjection
{
    public static class ApiServiceRegistration
    {
        public static IServiceCollection AddApi(this IServiceCollection services)
        {
            services
                .AddControllers()
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.InvalidModelStateResponseFactory = context =>
                    {
                        Dictionary<string, object> errors = context
                            .ModelState.Where(x => x.Value?.Errors.Count > 0)
                            .ToDictionary(
                                k => k.Key,
                                v => (object)v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                            );

                        return new BadRequestObjectResult(
                            new ApiErrorResponse
                            {
                                ErrorCode = "VALIDATION_ERROR",
                                Message = "One or more validation errors occurred.",
                                Details = errors,
                            }
                        );
                    };
                });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();

            return services;
        }
    }
}
