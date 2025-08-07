using marketplace_practice.Models;
using marketplace_practice.Services.interfaces;
using Microsoft.EntityFrameworkCore;

namespace marketplace_practice.Services
{
    public class LoyaltyService : ILoyaltyService
    {
        private readonly AppDbContext _context;

        public LoyaltyService(AppDbContext context)
        {
            _context = context;
        }

        // Получить аккаунт лояльности пользователя
        public async Task<LoyaltyAccount> GetAccountAsync(long userId)
        {
            return await _context.LoyaltyAccounts
                .FirstOrDefaultAsync(a => a.UserId == userId);
        }

        // Получить или создать аккаунт
        public async Task<LoyaltyAccount> GetOrCreateAccount(long userId)
        {
            var account = await GetAccountAsync(userId);
            if (account != null)
                return account;

            account = new LoyaltyAccount
            {
                UserId = userId,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.LoyaltyAccounts.Add(account);
            await _context.SaveChangesAsync();

            return account;
        }

        // Начислить баллы
        public async Task<(bool success, string message, LoyaltyAccount updatedAccount)> AddPoints(
            long userId,
            long orderId,
            long points,
            string description = "Начисление баллов")
        {
            if (points <= 0)
                return (false, "Количество баллов должно быть положительным", null);

            var account = await GetOrCreateAccount(userId);

            account.Balance += points;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new LoyaltyTransaction
            {
                UserId = userId,
                OrderId = orderId,
                Type = "CREDIT",
                Points = points,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return (true, "Баллы успешно начислены", account);
        }

        // Списать баллы
        public async Task<(bool success, string message, LoyaltyAccount updatedAccount)> DeductPoints(
            long userId,
            long orderId,
            long points,
            string description = "Списание баллов")
        {
            if (points <= 0)
                return (false, "Количество баллов должно быть положительным", null);

            var account = await GetAccountAsync(userId);
            if (account == null)
                return (false, "Аккаунт лояльности не найден", null);

            if (account.Balance < points)
                return (false, "Недостаточно баллов для списания", null);

            account.Balance -= points;
            account.UpdatedAt = DateTime.UtcNow;

            var transaction = new LoyaltyTransaction
            {
                UserId = userId,
                OrderId = orderId,
                Type = "DEBIT",
                Points = points,
                Description = description,
                CreatedAt = DateTime.UtcNow
            };

            _context.LoyaltyTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            return (true, "Баллы успешно списаны", account);
        }

        // Получить историю транзакций
        public async Task<List<LoyaltyTransaction>> GetTransactions(long userId)
        {
            return await _context.LoyaltyTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }
    }
}
