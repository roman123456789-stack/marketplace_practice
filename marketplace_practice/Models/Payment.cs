using marketplace_practice.Models.Enums;

namespace marketplace_practice.Models
{
    public class Payment
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public required string ProviderName { get; set; }
        public required string ProviderPaymentId { get; set; }
        public decimal Amount { get; set; }
        public Currency Currency { get; set; }
        public DateTime CreatedAt { get; set; }

        public Order Order { get; set; }
    }
}
