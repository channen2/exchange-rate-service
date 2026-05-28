using ExchangeRateService.Data;
using ExchangeRateService.Models;
using ExchangeRateService.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ExchangeRateService.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _db;

        public TransactionService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PurchaseTransaction> CreateAsync(
            decimal amount,
            DateTime transactionDate,
            string description
        )
        {
            PurchaseTransaction transaction = new PurchaseTransaction
            {
                Id = Guid.NewGuid(),
                Description = description,
                PurchaseAmountUsd = amount,
                TransactionDate = transactionDate,
                CreatedAt = DateTime.UtcNow,
            };

            _db.PurchaseTransactions.Add(transaction);
            await _db.SaveChangesAsync();

            return transaction;
        }

        public async Task<List<PurchaseTransaction>> GetAllAsync()
        {
            return await _db
                .PurchaseTransactions.OrderByDescending(x => x.TransactionDate)
                .ToListAsync();
        }

        public async Task<PurchaseTransaction?> GetByIdAsync(Guid id)
        {
            return await _db.PurchaseTransactions.FindAsync(id);
        }
    }
}
