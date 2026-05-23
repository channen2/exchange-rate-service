using ExchangeRateService.Models;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options) { }

        public DbSet<PurchaseTransaction> PurchaseTransactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .Entity<PurchaseTransaction>()
                .Property(x => x.PurchaseAmountUsd)
                .HasPrecision(18, 2);
        }
    }
}
