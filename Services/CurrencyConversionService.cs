using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.DTOs;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Services
{
    public class CurrencyConversionService : ICurrencyConversionService
    {
        private readonly ITransactionService _transactionService;
        private readonly ITreasuryExchangeRateService _exchangeRateService;

        public CurrencyConversionService(
            ITransactionService transactionService,
            ITreasuryExchangeRateService exchangeRateService
        )
        {
            _transactionService = transactionService;
            _exchangeRateService = exchangeRateService;
        }

        public async Task<Result<ConvertedTransactionResponse>> ConvertAsync(
            Guid transactionId,
            string targetCurrency
        )
        {
            PurchaseTransaction? transaction = await _transactionService.GetByIdAsync(
                transactionId
            );

            if (transaction is null)
            {
                return Result<ConvertedTransactionResponse>.Failure(
                    Errors.TransactionNotFound,
                    new Dictionary<string, object> { ["transactionId"] = transactionId.ToString() }
                );
            }

            Result<decimal> rateResult = await _exchangeRateService.GetExchangeRateAsync(
                targetCurrency,
                transaction.TransactionDate
            );

            if (!rateResult.IsSuccess)
            {
                return Result<ConvertedTransactionResponse>.Failure(
                    rateResult.Error!,
                    rateResult.Details
                );
            }

            decimal rate = rateResult.Value;

            decimal convertedAmount = Math.Round(
                transaction.PurchaseAmountUsd * rate,
                2,
                MidpointRounding.AwayFromZero
            );

            return Result<ConvertedTransactionResponse>.Success(
                new ConvertedTransactionResponse
                {
                    Id = transaction.Id,
                    Description = transaction.Description,
                    TransactionDate = transaction.TransactionDate,
                    OriginalPurchaseAmountUsd = transaction.PurchaseAmountUsd,
                    CurrencyCode = targetCurrency,
                    ExchangeRate = rate,
                    ConvertedAmount = convertedAmount,
                }
            );
        }
    }
}
