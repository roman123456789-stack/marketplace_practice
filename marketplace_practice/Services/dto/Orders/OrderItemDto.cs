using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Products;

namespace marketplace_practice.Services.dto.Orders
{
    public class OrderItemDto
    {
        public int Quantity { get; set; }
        public required string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public required ProductBriefInfoDto Product { get; set; }
    }
}
