using marketplace_practice.Models.Enums;

namespace marketplace_practice.Models
{
    public class Product
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public Currency Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
        public ICollection<Group> Groups { get; set; } = new List<Group>();
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public CartItem? CartItem { get; set; }
    }
}
