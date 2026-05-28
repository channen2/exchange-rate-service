namespace ExchangeRateService.Common.Errors
{
    public static class ErrorRegistry
    {
        public static readonly ErrorDefinition TransactionNotFound = new(
            "TRANSACTION_NOT_FOUND",
            404,
            "Transaction not found"
        );

        public static readonly ErrorDefinition UnsupportedCurrency = new(
            "UNSUPPORTED_CURRENCY",
            400,
            "Unsupported currency"
        );

        public static readonly ErrorDefinition ExchangeRateNotFound = new(
            "EXCHANGE_RATE_NOT_FOUND",
            422,
            "No exchange rate found within 6 months of transaction date"
        );

        public static readonly ErrorDefinition ExternalServiceError = new(
            "EXTERNAL_SERVICE_ERROR",
            502,
            "Failed to retrieve data from external exchange rate provider"
        );

        public static readonly ErrorDefinition TreasuryPaginationLimitExceeded = new(
            "TREASURY_PAGINATION_LIMIT_EXCEEDED",
            502,
            "Treasury API pagination limit exceeded during ingestion"
        );

        public static readonly ErrorDefinition ExchangeRateParseError = new(
            "EXCHANGE_RATE_PARSE_ERROR",
            502,
            "Invalid response received from exchange rate provider"
        );

        public static readonly ErrorDefinition ConversionFailed = new(
            "CONVERSION_FAILED",
            500,
            "An unexpected error occurred during conversion"
        );
    }
}
