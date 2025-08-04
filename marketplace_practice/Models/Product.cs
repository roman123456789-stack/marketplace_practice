using marketplace_practice.Models.Enums;

namespace marketplace_practice.Models
{
    public class Product
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public Currency Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; }
        public ICollection<Group> Groups { get; set; }
        public ICollection<FavoriteProduct> FavoriteProducts { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public ICollection<ProductImage> ProductImages { get; set; }
    }
}
