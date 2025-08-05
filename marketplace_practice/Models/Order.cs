using marketplace_practice.Models.Enums;

namespace marketplace_practice.Models
{
    public class Order
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; }
        public LoyaltyTransaction LoyaltyTransaction { get; set; }
        public Payment Payment { get; set; }
        public OrderDetail OrderDetail { get; set; }
    }
}
