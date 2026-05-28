using ExchangeRateService.Common;
using ExchangeRateService.Common.Errors;
using ExchangeRateService.DTOs.Responses;
using ExchangeRateService.Services.Interfaces;

namespace ExchangeRateService.Services
{
    public class CurrencyConversionService(
        ITransactionService transactionService,
        IExchangeRateProvider exchangeRateProvider,
        ITreasuryCurrencyMapper treasuryCurrencyMapper
    ) : ICurrencyConversionService
    {
        private readonly ITransactionService _transactionService = transactionService;
        private readonly IExchangeRateProvider _exchangeRateProvider = exchangeRateProvider;
        private readonly ITreasuryCurrencyMapper _treasuryCurrencyMapper = treasuryCurrencyMapper;

        public async Task<Result<ConvertedTransactionResponse>> ConvertAsync(
            Guid transactionId,
            string targetCurrency
        )
        {
            var transaction = await _transactionService.GetByIdAsync(transactionId);

            if (transaction is null)
            {
                return Result<ConvertedTransactionResponse>.Failure(
                    ErrorRegistry.TransactionNotFound,
                    new Dictionary<string, object> { ["transactionId"] = transactionId.ToString() }
                );
            }

            if (!_treasuryCurrencyMapper.TryToTreasury(targetCurrency, out var treasuryCurrency))
            {
                return Result<ConvertedTransactionResponse>.Failure(
                    ErrorRegistry.UnsupportedCurrency,
                    new Dictionary<string, object> { ["currency"] = targetCurrency }
                );
            }

            var rateResult = await _exchangeRateProvider.GetRateAsync(
                treasuryCurrency,
                transaction.TransactionDate
            );

            if (!rateResult.IsSuccess)
            {
                return Result<ConvertedTransactionResponse>.Failure(
                    rateResult.Error!,
                    rateResult.Details
                );
            }

            var rate = rateResult.Value;

            var convertedAmount = Math.Round(
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
                    CurrencyCode = targetCurrency.ToUpperInvariant(),
                    ExchangeRate = rate,
                    ConvertedAmount = convertedAmount,
                }
            );
        }
    }
}
