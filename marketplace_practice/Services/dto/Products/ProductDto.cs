using marketplace_practice.Models.Enums;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.dto.Users;

namespace marketplace_practice.Services.dto.Products
{
    public class ProductDto
    {
        public long Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public required UserBriefInfoDto Owner { get; set; }
        public Currency Currency { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public required ICollection<GroupDto> Groups { get; set; }
        public ICollection<ProductImageDto>? ProductImages { get; set; }
        public bool IsFavirite { get; set; }
        public bool IsAdded { get; set; }
    }
}
