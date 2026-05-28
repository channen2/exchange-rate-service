using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.Controllers;
using ExchangeRateService.DTOs.Requests;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;

namespace ExchangeRateService.Tests.Controllers
{
    public class TransactionsControllerTests
    {
        private readonly ITransactionService _transactionService;
        private readonly ICurrencyConversionService _conversionService;
        private readonly TransactionsController _controller;

        public TransactionsControllerTests()
        {
            _transactionService = Substitute.For<ITransactionService>();
            _conversionService = Substitute.For<ICurrencyConversionService>();

            _controller = new TransactionsController(_transactionService, _conversionService);
        }

        [Fact]
        public async Task GetAll_ShouldReturnOkWithTransactions()
        {
            // Arrange
            _transactionService
                .GetAllAsync()
                .Returns([
                    new()
                    {
                        Id = Guid.NewGuid(),
                        Description = "Test",
                        PurchaseAmountUsd = 100,
                        TransactionDate = new DateTime(2026, 1, 1),
                    },
                ]);

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsAssignableFrom<IEnumerable<TransactionResponse>>(okResult.Value);
            Assert.Single(value);
            Assert.Equal("Test", value.First().Description);
        }

        [Fact]
        public async Task GetById_ShouldReturnOk_WhenTransactionExists()
        {
            // Arrange
            var id = Guid.NewGuid();

            _transactionService
                .GetByIdAsync(id)
                .Returns(
                    new PurchaseTransaction
                    {
                        Id = id,
                        Description = "Laptop",
                        PurchaseAmountUsd = 100,
                        TransactionDate = new DateTime(2026, 1, 1),
                    }
                );

            // Act
            var result = await _controller.GetById(id);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<TransactionResponse>(okResult.Value);

            Assert.Equal(id, value.Id);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenMissing()
        {
            // Arrange
            var id = Guid.NewGuid();

            _transactionService.GetByIdAsync(id).Returns((PurchaseTransaction?)null);

            // Act
            var result = await _controller.GetById(id);

            // Assert
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Create_ShouldReturnOk_WithCreatedTransaction()
        {
            // Arrange
            var request = new CreateTransactionRequest
            {
                Description = "Laptop",
                PurchaseAmountUsd = 100,
                TransactionDate = new DateTime(2026, 1, 1),
            };

            var transaction = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                Description = request.Description,
                PurchaseAmountUsd = request.PurchaseAmountUsd,
                TransactionDate = request.TransactionDate.Value,
            };

            _transactionService
                .CreateAsync(
                    request.PurchaseAmountUsd,
                    request.TransactionDate.Value,
                    request.Description
                )
                .Returns(transaction);

            // Act
            var result = await _controller.Create(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<TransactionResponse>(okResult.Value);

            Assert.Equal(transaction.Id, value.Id);
            Assert.Equal(request.Description, value.Description);
            Assert.Equal(request.PurchaseAmountUsd, value.PurchaseAmountUsd);
        }

        [Fact]
        public async Task Convert_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var id = Guid.NewGuid();

            _conversionService
                .ConvertAsync(id, "CAD")
                .Returns(
                    Result<ConvertedTransactionResponse>.Success(
                        new ConvertedTransactionResponse
                        {
                            Id = id,
                            CurrencyCode = "CAD",
                            ExchangeRate = 1.25m,
                            ConvertedAmount = 125m,
                            TransactionDate = new DateTime(2026, 1, 1),
                            Description = "Laptop",
                            OriginalPurchaseAmountUsd = 100,
                        }
                    )
                );

            // Act
            var result = await _controller.Convert(id, "CAD");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var value = Assert.IsType<ConvertedTransactionResponse>(okResult.Value);

            Assert.Equal("CAD", value.CurrencyCode);
        }

        [Fact]
        public async Task Convert_ShouldReturnMappedError_WhenFailure()
        {
            // Arrange
            var id = Guid.NewGuid();

            _conversionService
                .ConvertAsync(id, "CAD")
                .Returns(
                    Result<ConvertedTransactionResponse>.Failure(ErrorRegistry.TransactionNotFound)
                );

            // Act
            var result = await _controller.Convert(id, "CAD");

            // Assert
            var objResult = Assert.IsType<ObjectResult>(result);
            var error = Assert.IsType<ApiErrorResponse>(objResult.Value);

            Assert.Equal(ErrorRegistry.TransactionNotFound.Code, error.ErrorCode);
        }
    }
}
