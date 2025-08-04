using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Models
{
    public class User : IdentityUser<long>
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsVerified { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<Product> Products { get; set; }
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; }
        public LoyaltyAccount LoyaltyAccount { get; set; }
        public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; }
        public ICollection<Order> Orders { get; set; }
    }
}
