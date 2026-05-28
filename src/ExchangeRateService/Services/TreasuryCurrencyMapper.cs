using ExchangeRateService.Configuration;
using ExchangeRateService.Services.Interfaces;
using Microsoft.Extensions.Options;

public class TreasuryCurrencyMapper : ITreasuryCurrencyMapper
{
    private readonly Dictionary<string, string> _toTreasury;
    private readonly Dictionary<string, string> _toInternal;

    public TreasuryCurrencyMapper(IOptions<TreasuryCurrencyOptions> options)
    {
        _toTreasury = new Dictionary<string, string>(
            options.Value.CurrencyMappings,
            StringComparer.OrdinalIgnoreCase
        );

        _toInternal = _toTreasury.ToDictionary(x => x.Value, x => x.Key);
    }

    public string ToTreasury(string currencyCode)
    {
        return _toTreasury.TryGetValue(currencyCode, out var treasuryCurrency)
            ? treasuryCurrency
            : currencyCode.ToUpperInvariant();
    }

    public string ToInternal(string treasuryCurrency)
    {
        return _toInternal.TryGetValue(treasuryCurrency, out var currencyCode)
            ? currencyCode
            : treasuryCurrency;
    }

    public bool TryToTreasury(string currencyCode, out string treasuryCurrency)
    {
        return _toTreasury.TryGetValue(currencyCode, out treasuryCurrency!);
    }

    public bool TryToInternal(string treasuryCurrency, out string currencyCode)
    {
        return _toInternal.TryGetValue(treasuryCurrency, out currencyCode!);
    }
}
