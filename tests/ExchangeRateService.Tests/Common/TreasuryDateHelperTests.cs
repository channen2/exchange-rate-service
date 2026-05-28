using ExchangeRateService.Common;

namespace ExchangeRateService.Tests.Common
{
    public class TreasuryDateHelperTests
    {
        [Theory]
        [InlineData(2026, 1, 15, 2026, 1, 1, 2026, 3, 31)]
        [InlineData(2026, 4, 10, 2026, 4, 1, 2026, 6, 30)]
        [InlineData(2026, 7, 10, 2026, 7, 1, 2026, 9, 30)]
        [InlineData(2026, 10, 10, 2026, 10, 1, 2026, 12, 31)]
        public void GetQuarterWindow_ShouldReturnCorrectQuarter(
            int year,
            int month,
            int day,
            int expFromY,
            int expFromM,
            int expFromD,
            int expToY,
            int expToM,
            int expToD
        )
        {
            // Arrange
            var input = new DateTime(year, month, day);

            // Act
            var (from, to) = TreasuryDateHelper.GetQuarterWindow(input);

            // Assert
            Assert.Equal(new DateTime(expFromY, expFromM, expFromD), from);
            Assert.Equal(new DateTime(expToY, expToM, expToD), to);
        }

        [Fact]
        public void GetQuarterWindows_ShouldReturnMultipleQuarters()
        {
            // Arrange
            var start = new DateTime(2026, 1, 1);
            var end = new DateTime(2026, 9, 30);

            // Act
            var result = TreasuryDateHelper.GetQuarterWindows(start, end).ToList();

            // Arrange
            Assert.Equal(3, result.Count);

            Assert.Equal(
                (2026, 1, 1),
                (result[0].from.Year, result[0].from.Month, result[0].from.Day)
            );
            Assert.Equal((2026, 3, 31), (result[0].to.Year, result[0].to.Month, result[0].to.Day));

            Assert.Equal(
                (2026, 4, 1),
                (result[1].from.Year, result[1].from.Month, result[1].from.Day)
            );
            Assert.Equal((2026, 6, 30), (result[1].to.Year, result[1].to.Month, result[1].to.Day));
        }

        [Theory]
        [InlineData(2026, 1, 15, 2025, 12, 31)]
        [InlineData(2026, 4, 15, 2026, 3, 31)]
        [InlineData(2026, 7, 15, 2026, 6, 30)]
        [InlineData(2026, 10, 15, 2026, 9, 30)]
        public void GetTreasuryRecordDate_ShouldReturnCorrectQuarterEnd(
            int year,
            int month,
            int day,
            int expYear,
            int expMonth,
            int expDay
        )
        {
            // Arrange
            var input = new DateTime(year, month, day);

            // Act
            var result = TreasuryDateHelper.GetTreasuryRecordDate(input);

            // Assert
            Assert.Equal(new DateTime(expYear, expMonth, expDay), result);
        }

        [Fact]
        public void GetPreviousTreasuryRecordDate_ShouldSubtractThreeMonths()
        {
            // Arrange
            var input = new DateTime(2026, 6, 30);

            // Act
            var result = TreasuryDateHelper.GetPreviousTreasuryRecordDate(input);

            // Assert
            Assert.Equal(new DateTime(2026, 3, 30), result);
        }
    }
}
