namespace marketplace_practice.Services.dto.Products
{
    public class ProductBriefInfoDto
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public required string Name { get; set; }
        public decimal Price { get; set; }
        public decimal? PromotionalPrice { get; set; }
        public required string Currency { get; set; }
        public ICollection<ProductImageDto>? ProductImages { get; set; }
        public required DateTime CreatedAt { get; set; }
        public bool IsFavirite { get; set; }
        public bool IsAdded { get; set; }
    }
}
