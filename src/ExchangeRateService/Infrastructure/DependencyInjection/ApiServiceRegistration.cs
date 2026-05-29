using System.Threading.RateLimiting;
using ExchangeRateService.DTOs.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Filters;

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

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc(
                    "v1",
                    new()
                    {
                        Title = "Exchange Rate Service",
                        Version = "v1",
                        Description =
                            "Service for currency conversion using Treasury API exchange rates",
                    }
                );
                var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

                c.IncludeXmlComments(xmlPath);
                c.ExampleFilters();
            });

            services.AddSwaggerExamplesFromAssemblyOf<Program>();

            services.AddRateLimiter(options =>
            {
                options.AddPolicy(
                    "standard",
                    httpContext =>
                    {
                        var partitionKey =
                            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                        return RateLimitPartition.GetSlidingWindowLimiter(
                            partitionKey: partitionKey,
                            factory: _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromSeconds(10),
                                SegmentsPerWindow = 3,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                                QueueLimit = 10,
                            }
                        );
                    }
                );

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return services;
        }
    }
}
