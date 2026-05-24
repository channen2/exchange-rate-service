using ExchangeRateService.Common;
using ExchangeRateService.DTOs;
using ExchangeRateService.Models;
using ExchangeRateService.Services;
using ExchangeRateService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ExchangeRateService.Controllers
{
    [ApiController]
    [Route("api/transactions")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICurrencyConversionService _conversionService;

        public TransactionsController(
            ITransactionService transactionService,
            ICurrencyConversionService conversionService
        )
        {
            _transactionService = transactionService;
            _conversionService = conversionService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            List<PurchaseTransaction> transactions = await _transactionService.GetAllAsync();

            IEnumerable<TransactionResponse> response = transactions.Select(
                x => new TransactionResponse
                {
                    Id = x.Id,
                    Description = x.Description,
                    PurchaseAmountUsd = x.PurchaseAmountUsd,
                    TransactionDate = x.TransactionDate,
                }
            );

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            PurchaseTransaction? transaction = await _transactionService.GetByIdAsync(id);

            if (transaction is null)
            {
                return NotFound();
            }

            TransactionResponse response = new()
            {
                Id = transaction.Id,
                Description = transaction.Description,
                PurchaseAmountUsd = transaction.PurchaseAmountUsd,
                TransactionDate = transaction.TransactionDate,
            };

            return Ok(response);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAsync(CreateTransactionRequest request)
        {
            PurchaseTransaction transaction = await _transactionService.Create(
                request.PurchaseAmountUsd,
                request.TransactionDate!.Value,
                request.Description
            );

            TransactionResponse response = new()
            {
                Id = transaction.Id,
                Description = transaction.Description,
                PurchaseAmountUsd = transaction.PurchaseAmountUsd,
                TransactionDate = transaction.TransactionDate,
            };

            return Ok(response);
        }

        [HttpGet("{id}/convert")]
        public async Task<IActionResult> Convert(Guid id, [FromQuery] string currency)
        {
            Result<ConvertedTransactionResponse> result = await _conversionService.ConvertAsync(
                id,
                currency
            );

            if (!result.IsSuccess)
            {
                return MapError(result.Error!);
            }

            return Ok(result.Value);
        }

        private IActionResult MapError(string errorCode)
        {
            return errorCode switch
            {
                ErrorCodes.TransactionNotFound => NotFound(
                    new ApiErrorResponse
                    {
                        ErrorCode = errorCode,
                        Message = "Transaction not found",
                    }
                ),

                ErrorCodes.UnsupportedCurrency => BadRequest(
                    new ApiErrorResponse { ErrorCode = errorCode, Message = "Unsupported currency" }
                ),

                ErrorCodes.ExchangeRateNotFound => UnprocessableEntity(
                    new ApiErrorResponse
                    {
                        ErrorCode = errorCode,
                        Message = "No exchange rate found within 6 months of transaction date",
                    }
                ),

                ErrorCodes.ExchangeRateParseError => StatusCode(
                    502,
                    new ApiErrorResponse
                    {
                        ErrorCode = errorCode,
                        Message = "Invalid response received from exchange rate provider",
                    }
                ),

                ErrorCodes.ExchangeRateApiEmptyResponse => StatusCode(
                    502,
                    new ApiErrorResponse
                    {
                        ErrorCode = errorCode,
                        Message = "Exchange rate provider returned no data",
                    }
                ),

                _ => StatusCode(
                    500,
                    new ApiErrorResponse
                    {
                        ErrorCode = ErrorCodes.ConversionFailed,
                        Message = "An unexpected error occurred during conversion",
                    }
                ),
            };
        }
    }
}
