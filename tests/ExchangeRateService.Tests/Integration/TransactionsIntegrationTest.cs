using System.Net;
using System.Net.Http.Json;
using ExchangeRateService.DTOs.Requests;
using ExchangeRateService.DTOs.Responses;

namespace ExchangeRateService.Tests.Integration
{
    public class TransactionsIntegrationTests(CustomWebApplicationFactory factory)
        : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client = factory.CreateClient();

        [Fact]
        public async Task FullFlow_CreateAndConvertTransaction_ShouldReturnConvertedValue()
        {
            // Arrange
            var currencyCode = await GetValidCurrencyCodeAsync();

            var createRequest = new CreateTransactionRequest
            {
                Description = "Laptop",
                PurchaseAmountUsd = 100m,
                TransactionDate = new DateTime(2026, 1, 1),
            };

            // Create Transaction
            var createResponse = await _client.PostAsJsonAsync("/api/transactions", createRequest);

            createResponse.EnsureSuccessStatusCode();

            var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

            Assert.NotNull(created);

            // Convert Transaction
            var convertResponse = await _client.GetAsync(
                $"/api/transactions/{created.Id}/convert?currency={currencyCode}"
            );

            convertResponse.EnsureSuccessStatusCode();

            var converted =
                await convertResponse.Content.ReadFromJsonAsync<ConvertedTransactionResponse>();

            // Assert
            Assert.NotNull(converted);

            Assert.Equal(currencyCode, converted.CurrencyCode);
            Assert.True(converted.ConvertedAmount > 0);
            Assert.Equal(100m, converted.OriginalPurchaseAmountUsd);
        }

        [Fact]
        public async Task Convert_ShouldReturnNotFound_WhenTransactionDoesNotExist()
        {
            // Arrange
            var currencyCode = await GetValidCurrencyCodeAsync();
            var transactionId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync(
                $"/api/transactions/{transactionId}/convert?currency={currencyCode}"
            );

            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            Assert.Contains("TRANSACTION_NOT_FOUND", content);
        }

        [Fact]
        public async Task Convert_ShouldReturnBadRequest_WhenCurrencyIsUnsupported()
        {
            // Arrange
            var createResponse = await _client.PostAsJsonAsync(
                "/api/transactions",
                new
                {
                    description = "Laptop",
                    purchaseAmountUsd = 100m,
                    transactionDate = DateTime.UtcNow,
                }
            );

            var invalidCurrency = "XYZ";
            var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

            // Act
            var response = await _client.GetAsync(
                $"/api/transactions/{created!.Id}/convert?currency={invalidCurrency}"
            );

            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            Assert.Contains("UNSUPPORTED_CURRENCY", content);
        }

        [Fact]
        public async Task GetById_ShouldReturnCreatedTransaction()
        {
            // Arrange
            var createResponse = await _client.PostAsJsonAsync(
                "/api/transactions",
                new
                {
                    description = "Laptop",
                    purchaseAmountUsd = 100m,
                    transactionDate = DateTime.UtcNow,
                }
            );

            var created = await createResponse.Content.ReadFromJsonAsync<TransactionResponse>();

            // Act
            var response = await _client.GetAsync($"/api/transactions/{created!.Id}");

            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<TransactionResponse>();

            // Assert
            Assert.NotNull(transaction);
            Assert.Equal(created.Id, transaction.Id);
            Assert.Equal(created.Description, transaction.Description);
        }

        private async Task<string> GetValidCurrencyCodeAsync()
        {
            var response = await _client.GetAsync("/api/currencies/");

            response.EnsureSuccessStatusCode();

            var currencies = await response.Content.ReadFromJsonAsync<
                List<SupportedCurrencyResponse>
            >();

            Assert.NotNull(currencies);
            Assert.NotEmpty(currencies);

            return currencies[0].Code;
        }
    }
}
