using ExchangeRateService.Data;
using ExchangeRateService.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

IWebHostEnvironment env = builder.Environment;

builder.Services.AddApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, env);
builder.Services.AddBackgroundWorkers(env);

WebApplication app = builder.Build();

// Apply migrations on startup
if (!app.Environment.IsEnvironment("Testing"))
{
    using IServiceScope scope = app.Services.CreateScope();
    AppDbContext db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Exchange Rate Service v1");
        c.RoutePrefix = string.Empty; // makes Swagger load at "/"
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.Run();
