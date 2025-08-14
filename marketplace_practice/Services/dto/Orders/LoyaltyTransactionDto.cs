namespace marketplace_practice.Services.dto.Orders
{
    public class LoyaltyTransactionDto
    {
        public long UserId { get; set; }
        public required string Type { get; set; }
        public long Points { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
