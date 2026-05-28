using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Models;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using NSubstitute;

namespace ExchangeRateService.Tests.Services
{
    public class CurrencyConversionServiceTests
    {
        private const string CadCurrency = "CAD";
        private const string TreasuryCadCurrency = "Canada-Dollar";
        private readonly ITransactionService _transactionService;
        private readonly IExchangeRateProvider _exchangeRateProvider;
        private readonly ITreasuryCurrencyMapper _currencyMapper;

        private readonly CurrencyConversionService _service;

        public CurrencyConversionServiceTests()
        {
            _transactionService = Substitute.For<ITransactionService>();
            _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
            _currencyMapper = Substitute.For<ITreasuryCurrencyMapper>();

            _service = new CurrencyConversionService(
                _transactionService,
                _exchangeRateProvider,
                _currencyMapper
            );
        }

        [Fact]
        public async Task ConvertAsync_ShouldReturnConvertedTransaction_WhenSuccessful()
        {
            // Arrange
            Guid transactionId = Guid.NewGuid();

            PurchaseTransaction transaction = CreateTransaction(
                transactionId,
                100m,
                new DateTime(2026, 1, 1),
                "Laptop"
            );

            _transactionService.GetByIdAsync(transactionId).Returns(transaction);

            string treasuryCurrency = TreasuryCadCurrency;

            SetupCurrencyMapping(CadCurrency, treasuryCurrency);

            _exchangeRateProvider
                .GetRateAsync(treasuryCurrency, transaction.TransactionDate)
                .Returns(Result<decimal>.Success(1.25m));

            // Act
            var result = await _service.ConvertAsync(transactionId, CadCurrency);

            // Assert
            Assert.True(result.IsSuccess);

            Assert.NotNull(result.Value);
            Assert.Equal(125m, result.Value.ConvertedAmount);
            Assert.Equal(CadCurrency, result.Value.CurrencyCode);
            Assert.Equal(1.25m, result.Value.ExchangeRate);
        }

        [Fact]
        public async Task ConvertAsync_ShouldReturnFailure_WhenTransactionDoesNotExist()
        {
            // Arrange
            Guid transactionId = Guid.NewGuid();

            _transactionService.GetByIdAsync(transactionId).Returns((PurchaseTransaction?)null);

            // Act
            var result = await _service.ConvertAsync(transactionId, CadCurrency);

            // Assert
            Assert.False(result.IsSuccess);

            Assert.Equal(ErrorRegistry.TransactionNotFound.Code, result.Error!.Code);
        }

        [Fact]
        public async Task ConvertAsync_ShouldReturnFailure_WhenCurrencyIsUnsupported()
        {
            // Arrange
            Guid transactionId = Guid.NewGuid();

            PurchaseTransaction transaction = CreateTransaction(transactionId, 100m);

            _transactionService.GetByIdAsync(transactionId).Returns(transaction);

            _currencyMapper.TryToTreasury("XYZ", out var _).Returns(false);

            // Act
            var result = await _service.ConvertAsync(transactionId, "XYZ");

            // Assert
            Assert.False(result.IsSuccess);

            Assert.Equal(ErrorRegistry.UnsupportedCurrency.Code, result.Error!.Code);
        }

        [Fact]
        public async Task ConvertAsync_ShouldPropagateExchangeRateProviderFailure()
        {
            // Arrange
            Guid transactionId = Guid.NewGuid();

            PurchaseTransaction transaction = CreateTransaction(transactionId, 100m);

            _transactionService.GetByIdAsync(transactionId).Returns(transaction);

            string treasuryCurrency = TreasuryCadCurrency;

            SetupCurrencyMapping(CadCurrency, treasuryCurrency);

            _exchangeRateProvider
                .GetRateAsync(treasuryCurrency, transaction.TransactionDate)
                .Returns(Result<decimal>.Failure(ErrorRegistry.ExchangeRateNotFound));

            // Act
            var result = await _service.ConvertAsync(transactionId, CadCurrency);

            // Assert
            Assert.False(result.IsSuccess);

            Assert.Equal(ErrorRegistry.ExchangeRateNotFound.Code, result.Error!.Code);
        }

        [Fact]
        public async Task ConvertAsync_ShouldRoundUsingAwayFromZero()
        {
            // Arrange
            Guid transactionId = Guid.NewGuid();

            PurchaseTransaction transaction = CreateTransaction(transactionId, 10.005m);

            _transactionService.GetByIdAsync(transactionId).Returns(transaction);

            string treasuryCurrency = TreasuryCadCurrency;

            SetupCurrencyMapping(CadCurrency, treasuryCurrency);

            _exchangeRateProvider
                .GetRateAsync(treasuryCurrency, transaction.TransactionDate)
                .Returns(Result<decimal>.Success(1m));

            // Act
            var result = await _service.ConvertAsync(transactionId, CadCurrency);

            // Assert
            Assert.True(result.IsSuccess);

            Assert.NotNull(result.Value);
            Assert.Equal(10.01m, result.Value.ConvertedAmount);
        }

        private void SetupCurrencyMapping(string currencyCode, string treasuryCurrency)
        {
            _currencyMapper
                .TryToTreasury(currencyCode, out Arg.Any<string>())
                .Returns(callInfo =>
                {
                    callInfo[1] = treasuryCurrency;
                    return true;
                });
        }

        private static PurchaseTransaction CreateTransaction(
            Guid id,
            decimal amount,
            DateTime? transactionDate = null,
            string description = "Test Transaction"
        )
        {
            return new PurchaseTransaction
            {
                Id = id,
                Description = description,
                PurchaseAmountUsd = amount,
                TransactionDate = transactionDate ?? new DateTime(2026, 1, 1),
            };
        }
    }
}
