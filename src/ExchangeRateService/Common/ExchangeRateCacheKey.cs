namespace ExchangeRateService.Common
{
    public static class ExchangeRateCacheKey
    {
        public static string FromRecordDate(string treasuryCurrency, DateTime recordDate)
        {
            return $"{treasuryCurrency}|{recordDate:yyyy-MM-dd}";
        }

        public static string FromTransactionDate(string treasuryCurrency, DateTime transactionDate)
        {
            var recordDate = TreasuryDateHelper.GetTreasuryRecordDate(transactionDate);
            return FromRecordDate(treasuryCurrency, recordDate);
        }
    }
}
