using marketplace_practice.Models.Enums;

namespace marketplace_practice.Models
{
    public class OrderItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long CartItemId { get; set; }
        public int Quantity { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Order Order { get; set; }
        public CartItem CartItem { get; set; }
    }
}
