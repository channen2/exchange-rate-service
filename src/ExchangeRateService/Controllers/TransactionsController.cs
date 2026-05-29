using System.ComponentModel.DataAnnotations;
using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.DTOs.Requests;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using ExchangeRateService.Swagger.Examples;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Filters;

namespace ExchangeRateService.Controllers
{
    [EnableRateLimiting("standard")]
    [ApiController]
    [Route("api/v1/transactions")]
    [Tags("Transactions")]
    public class TransactionsController(
        ITransactionService transactionService,
        ICurrencyConversionService conversionService
    ) : ControllerBase
    {
        private readonly ITransactionService _transactionService = transactionService;
        private readonly ICurrencyConversionService _conversionService = conversionService;

        /// <summary>
        /// Retrieves all stored purchase transactions.
        /// </summary>
        /// <returns>
        /// A list of transaction records.
        /// </returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<TransactionResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TransactionResponse>>> GetAll()
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

        /// <summary>
        /// Retrieves a specific transaction by its unique identifier.
        /// </summary>
        /// <param name="id">The transaction ID.</param>
        /// <returns>The requested transaction if found.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransactionResponse>> GetById(Guid id)
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

        /// <summary>
        /// Creates a new purchase transaction in USD.
        /// </summary>
        /// <param name="request">The transaction creation payload.</param>
        /// <returns>The created transaction.</returns>
        [HttpPost]
        [SwaggerRequestExample(typeof(CreateTransactionRequest), typeof(CreateTransactionExample))]
        [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateTransactionRequest request)
        {
            PurchaseTransaction transaction = await _transactionService.CreateAsync(
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

            return CreatedAtAction(nameof(GetById), new { id = transaction.Id }, response);
        }

        /// <summary>
        /// Converts a stored transaction from USD into the requested currency
        /// using the most recent exchange rate available within 6 months of the transaction date.
        /// </summary>
        /// <param name="id">Transaction identifier.</param>
        /// <param name="currencyCode">Target ISO currency code (e.g. CAD, EUR).</param>
        /// <returns>The converted transaction with exchange rate and converted amount.</returns>
        [HttpGet("{id}/convert")]
        [ProducesResponseType(typeof(ConvertedTransactionResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Convert(Guid id, [FromQuery, Required] string currencyCode)
        {
            Result<ConvertedTransactionResponse> result = await _conversionService.ConvertAsync(
                id,
                currencyCode
            );

            if (!result.IsSuccess)
            {
                ErrorDefinition error = result.Error!;

                return StatusCode(
                    error.StatusCode,
                    new ApiErrorResponse
                    {
                        ErrorCode = error.Code,
                        Message = error.Message,
                        Details = result.Details,
                    }
                );
            }

            return Ok(result.Value);
        }
    }
}
