using System.Text.Json.Serialization;

namespace ExchangeRateService.DTOs.Treasury
{
    public class TreasuryExchangeRateRecord
    {
        [JsonPropertyName("country_currency_desc")]
        public string CountryCurrencyDescription { get; set; } = string.Empty;

        [JsonPropertyName("exchange_rate")]
        public string ExchangeRate { get; set; } = string.Empty;

        [JsonPropertyName("record_date")]
        public string RecordDate { get; set; } = string.Empty;
    }
}
