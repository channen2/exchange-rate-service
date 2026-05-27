namespace ExchangeRateService.Services.Interfaces
{
    public interface IExchangeRateRefreshOrchestrator
    {
        Task EnsureBootstrapAsync();

        Task RefreshRecentAsync();
    }
}
