namespace marketplace_practice.Services.dto.Orders
{
    public class PaymentDto
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public required string ProviderName { get; set; }
        public required string ProviderPaymentId { get; set; }
        public decimal Amount { get; set; }
        public required string Currency { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReceiptUrl { get; set; }
    }
}
