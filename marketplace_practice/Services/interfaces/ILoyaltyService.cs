using marketplace_practice.Models;

namespace marketplace_practice.Services.interfaces
{
    public interface ILoyaltyService
    {
        public Task<LoyaltyAccount> GetAccountAsync(long userId);
        public Task<LoyaltyAccount> GetOrCreateAccount(long userId);
        public Task<(bool success, string message, LoyaltyAccount updatedAccount)> AddPoints(
            long userId,
            long orderId,
            long points,
            string description);
        public Task<(bool success, string message, LoyaltyAccount updatedAccount)> DeductPoints(
            long userId,
            long orderId,
            long points,
            string description);
        public Task<List<LoyaltyTransaction>> GetTransactions(long userId);
    }
}
