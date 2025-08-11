using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Models
{
    public class User : IdentityUser<long>
    {
        public required string FirstName { get; set; }
        public string? LastName { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();
        public LoyaltyAccount? LoyaltyAccount { get; set; }
        public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();
        public ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}
