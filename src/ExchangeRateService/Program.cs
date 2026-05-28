using ExchangeRateService.Background;
using ExchangeRateService.Background.Interfaces;
using ExchangeRateService.Configuration;
using ExchangeRateService.Data;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Infrastructure;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .Services.AddControllers()
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

            ApiErrorResponse response = new()
            {
                ErrorCode = "VALIDATION_ERROR",
                Message = "One or more validation errors occurred.",
                Details = errors,
            };

            return new BadRequestObjectResult(response);
        };
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder
    .Services.AddHttpClient<ITreasuryExchangeRateApiClient, TreasuryExchangeRateApiClient>()
    .AddPolicyHandler(PollyPolicies.GetRetryPolicy())
    .AddPolicyHandler(PollyPolicies.GetCircuitBreakerPolicy());
builder.Services.AddScoped<ICurrencyConversionService, CurrencyConversionService>();
builder.Services.AddScoped<IExchangeRateProvider, ExchangeRateProvider>();
builder.Services.AddScoped<IExchangeRateIngestionService, ExchangeRateIngestionService>();

builder.Services.AddScoped<IExchangeRateRefreshOrchestrator, ExchangeRateRefreshOrchestrator>();
builder.Services.AddHostedService<ExchangeRateRefreshHostedService>();
builder.Services.AddSingleton<IExchangeRateIngestionBuffer, ExchangeRateIngestionBuffer>();
builder.Services.AddHostedService<ExchangeRateIngestionWorker>();

builder.Services.Configure<TreasuryCurrencyOptions>(
    builder.Configuration.GetSection("TreasuryCurrencyOptions")
);

builder.Services.AddSingleton<ITreasuryCurrencyMapper, TreasuryCurrencyMapper>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient();

WebApplication app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
