namespace ExchangeRateService.Common
{
    public static class ExchangeRateCacheKey
    {
        public static string FromRecordDate(string currency, DateTime recordDate)
        {
            return $"{currency.ToUpperInvariant()}|{recordDate:yyyy-MM-dd}";
        }

        public static string FromTransactionDate(string currency, DateTime transactionDate)
        {
            var recordDate = TreasuryDateHelper.GetTreasuryRecordDate(transactionDate);
            return FromRecordDate(currency, recordDate);
        }
    }
}
