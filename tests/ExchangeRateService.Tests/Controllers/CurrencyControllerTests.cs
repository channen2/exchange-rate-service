using ExchangeRateService.Controllers;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ExchangeRateService.Tests.Controllers
{
    public class CurrencyControllerTests
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencyController _controller;

        public CurrencyControllerTests()
        {
            _currencyService = Substitute.For<ICurrencyService>();
            _controller = new CurrencyController(_currencyService);
        }

        [Fact]
        public void GetSupported_ShouldReturnOk_WithCurrencies()
        {
            // Arrange
            _currencyService
                .GetSupportedCurrencies()
                .Returns([
                    new("EUR", "Euro Zone-Euro"),
                    new("GBP", "United Kingdom-Pound Sterling"),
                ]);

            // Act
            var result = _controller.GetSupported();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<SupportedCurrencyResponse>>(
                okResult.Value
            );

            Assert.Equal(2, value.Count());
        }

        [Fact]
        public void GetSupported_ShouldReturnOk_WhenEmpty()
        {
            // Arrange
            _currencyService.GetSupportedCurrencies().Returns([]);

            // Act
            var result = _controller.GetSupported();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsAssignableFrom<IEnumerable<SupportedCurrencyResponse>>(
                okResult.Value
            );

            Assert.Empty(value);
        }
    }
}
