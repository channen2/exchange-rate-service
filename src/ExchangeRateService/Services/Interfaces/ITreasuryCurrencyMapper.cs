namespace ExchangeRateService.Services.Interfaces
{
    public interface ITreasuryCurrencyMapper
    {
        string ToTreasury(string currencyCode);

        string ToInternal(string treasuryCurrency);

        bool TryToTreasury(string currencyCode, out string treasuryCurrency);

        bool TryToInternal(string treasuryCurrency, out string currencyCode);
    }
}
