namespace ExchangeRateService.Common
{
    public static class TreasuryDateHelper
    {
        public static IEnumerable<(DateTime from, DateTime to)> GetQuarterWindows(
            DateTime start,
            DateTime end
        )
        {
            var iterationDate = new DateTime(start.Year, start.Month, 1);

            while (iterationDate <= end)
            {
                var window = GetQuarterWindow(iterationDate);

                yield return window;

                iterationDate = window.to.AddDays(1);
            }
        }

        public static (DateTime from, DateTime to) GetQuarterWindow(DateTime date)
        {
            var quarter = ((date.Month - 1) / 3) + 1;

            var startMonth = ((quarter - 1) * 3) + 1;

            var from = new DateTime(date.Year, startMonth, 1);

            var to = from.AddMonths(3).AddDays(-1);

            return (from, to);
        }

        public static DateTime GetTreasuryRecordDate(DateTime date)
        {
            var quarter = ((date.Month - 1) / 3) + 1;

            return quarter switch
            {
                1 => new DateTime(date.Year - 1, 12, 31),
                2 => new DateTime(date.Year, 3, 31),
                3 => new DateTime(date.Year, 6, 30),
                4 => new DateTime(date.Year, 9, 30),
                _ => throw new InvalidOperationException(),
            };
        }

        public static DateTime GetPreviousTreasuryRecordDate(DateTime recordDate)
        {
            return recordDate.AddMonths(-3);
        }
    }
}
