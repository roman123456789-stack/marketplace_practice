using marketplace_practice.Services.dto.Products;

namespace marketplace_practice.Services.dto.Carts
{
    public class CartItemDto
    {
        public required string CartItemId { get; set; }
        public required int Quantity { get; set; }
        public required ProductBriefInfoDto productBriefInfo {  get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
