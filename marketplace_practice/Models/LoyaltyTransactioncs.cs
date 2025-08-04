namespace marketplace_practice.Models
{
    public class LoyaltyTransaction
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long OrderId { get; set; }
        public string Type { get; set; }
        public long Points { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
        public Order Order { get; set; }
    }
}
