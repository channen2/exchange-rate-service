using ExchangeRateService.Data;
using ExchangeRateService.Models;
using ExchangeRateService.Services;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Tests.Services
{
    public class TransactionServiceTests
    {
        private static AppDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Filename=:memory:")
                .Options;

            var db = new AppDbContext(options);
            db.Database.OpenConnection();
            db.Database.EnsureCreated();

            return db;
        }

        private static TransactionService CreateSut(AppDbContext db)
        {
            return new TransactionService(db);
        }

        [Fact]
        public async Task Create_ShouldPersistTransaction()
        {
            // Arrange
            var db = CreateDbContext();
            var sut = CreateSut(db);

            // Act
            var result = await sut.Create(100m, DateTime.UtcNow, "test");

            // Assert
            var transaction = await db.PurchaseTransactions.FindAsync(result.Id);

            Assert.NotNull(transaction);
            Assert.Equal(100m, transaction.PurchaseAmountUsd);
            Assert.Equal("test", transaction.Description);
        }

        [Fact]
        public async Task GetById_ShouldReturnTransaction_WhenExists()
        {
            // Arrange
            var db = CreateDbContext();

            var entity = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                Description = "test",
                PurchaseAmountUsd = 10,
                TransactionDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
            };

            db.PurchaseTransactions.Add(entity);
            await db.SaveChangesAsync();

            var sut = CreateSut(db);

            // Act
            var result = await sut.GetByIdAsync(entity.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(entity.Id, result.Id);
        }

        [Fact]
        public async Task GetAll_ShouldReturnDescendingByTransactionDate()
        {
            // Arrange
            var db = CreateDbContext();

            var older = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                TransactionDate = DateTime.UtcNow.AddDays(-1),
                PurchaseAmountUsd = 10,
            };

            var newer = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                TransactionDate = DateTime.UtcNow,
                PurchaseAmountUsd = 20,
            };

            db.PurchaseTransactions.AddRange(older, newer);
            await db.SaveChangesAsync();

            var sut = CreateSut(db);

            // Act
            var result = await sut.GetAllAsync();

            // Assert
            Assert.Equal(newer.Id, result[0].Id);
            Assert.Equal(older.Id, result[1].Id);
        }
    }
}
