namespace marketplace_practice.Models
{
    public class LoyaltyAccount
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long Balance { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public User User { get; set; }
    }
}
