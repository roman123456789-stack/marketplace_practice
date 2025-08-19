using marketplace_practice.Services.dto.Users;

namespace marketplace_practice.Services.dto.Orders
{
    public class OrderDto
    {
        public long Id { get; set; }
        public required UserBriefInfoDto User { get; set; }
        public decimal TotalAmount { get; set; }
        public required string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public ICollection<OrderItemDto> OrderItems { get; set; } = new List<OrderItemDto>();
        public LoyaltyTransactionDto? LoyaltyTransaction { get; set; }
        public PaymentDto? Payment { get; set; }
        public required OrderDetailDto OrderDetail { get; set; }
    }
}
