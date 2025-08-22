using marketplace_practice.Services.dto.Users;

namespace marketplace_practice.Services.dto.Products
{
    public class ProductDto
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public short? Size { get; set; }
        public int StockQuantity { get; set; }
        public required UserBriefInfoDto Owner { get; set; }
        public required string Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<ProductImageDto>? ProductImages { get; set; }
        public bool IsFavirite { get; set; }
        public bool IsAdded { get; set; }
    }
}
