namespace ExchangeRateService.Common
{
    public static class ErrorCodes
    {
        public const string TransactionNotFound = "TRANSACTION_NOT_FOUND";
        public const string UnsupportedCurrency = "UNSUPPORTED_CURRENCY";
        public const string ExchangeRateNotFound = "EXCHANGE_RATE_NOT_FOUND";
        public const string ConversionFailed = "CONVERSION_FAILED";
        public const string ExchangeRateParseError = "EXCHANGE_RATE_PARSE_ERROR";
        public const string ExchangeRateApiEmptyResponse = "EXCHANGE_RATE_API_EMPTY_RESPONSE";
    }
}
