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
        public decimal? PromotionalPrice { get; set; }
        public short? Size { get; set; }
        public int StockQuantity { get; set; }
        public Currency Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public User User { get; set; }
        public ICollection<Category> Categories { get; set; } = new List<Category>();
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; } = new List<FavoriteProduct>();
        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    }
}
