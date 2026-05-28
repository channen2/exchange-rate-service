using ExchangeRateService.Configuration;
using ExchangeRateService.Services;
using Microsoft.Extensions.Options;

namespace ExchangeRateService.Tests.Services
{
    public class CurrencyServiceTests
    {
        [Fact]
        public void GetSupportedCurrencies_ShouldReturnMappedCurrencies()
        {
            // Arrange
            var options = Options.Create(
                new TreasuryCurrencyOptions
                {
                    CurrencyMappings = new Dictionary<string, string>
                    {
                        { "EUR", "Euro Zone-Euro" },
                        { "GBP", "United Kingdom-Pound Sterling" },
                    },
                }
            );

            var service = new CurrencyService(options);

            // Act
            var result = service.GetSupportedCurrencies();

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Code == "EUR");
            Assert.Contains(result, x => x.Code == "GBP");
        }

        [Fact]
        public void GetSupportedCurrencies_ShouldReturnCurrenciesSortedByCode()
        {
            // Arrange
            var options = Options.Create(
                new TreasuryCurrencyOptions
                {
                    CurrencyMappings = new Dictionary<string, string>
                    {
                        { "USD", "United States-Dollar" },
                        { "AUD", "Australia-Dollar" },
                        { "EUR", "Euro Zone-Euro" },
                    },
                }
            );

            var service = new CurrencyService(options);

            // Act
            var result = service.GetSupportedCurrencies();

            // Assert
            Assert.Equal("AUD", result[0].Code);
            Assert.Equal("EUR", result[1].Code);
            Assert.Equal("USD", result[2].Code);
        }

        [Fact]
        public void GetSupportedCurrencies_ShouldReturnEmptyList_WhenNoMappingsExist()
        {
            // Arrange
            var options = Options.Create(new TreasuryCurrencyOptions { CurrencyMappings = [] });

            var service = new CurrencyService(options);

            // Act
            var result = service.GetSupportedCurrencies();

            // Assert
            Assert.Empty(result);
        }
    }
}
