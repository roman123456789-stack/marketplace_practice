using marketplace_practice.Models.Enums;
using marketplace_practice.Services.interfaces;

namespace marketplace_practice.Models
{
    public class User
    {
        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string PasswordHash { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Product> Products { get; set; }
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; }
        public LoyaltyAccount LoyaltyAccount { get; set; }
        public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public ICollection<Order> Orders { get; set; }
        public ICollection<Role> Roles { get; set; }
    }
}
