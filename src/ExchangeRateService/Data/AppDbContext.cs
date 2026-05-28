using ExchangeRateService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
    {
        public DbSet<PurchaseTransaction> PurchaseTransactions { get; set; }

        public DbSet<ExchangeRate> ExchangeRates { get; set; }

        public DbSet<IngestionRun> IngestionRuns { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<PurchaseTransaction>()
                .Property(x => x.PurchaseAmountUsd)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ExchangeRate>().Property(x => x.Rate).HasPrecision(18, 6);

            modelBuilder
                .Entity<ExchangeRate>()
                .HasIndex(x => new
                {
                    x.TreasuryCurrency,
                    x.EffectiveDate,
                    x.RecordDate,
                })
                .IsUnique();
        }
    }
}
