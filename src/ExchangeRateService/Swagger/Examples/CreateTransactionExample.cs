using ExchangeRateService.DTOs.Requests;
using Swashbuckle.AspNetCore.Filters;

namespace ExchangeRateService.Swagger.Examples
{
    public class CreateTransactionExample : IExamplesProvider<CreateTransactionRequest>
    {
        public CreateTransactionRequest GetExamples()
        {
            return new CreateTransactionRequest
            {
                PurchaseAmountUsd = 850.52m,
                TransactionDate = DateTime.UtcNow,
                Description = "Laptop purchase",
            };
        }
    }
}
