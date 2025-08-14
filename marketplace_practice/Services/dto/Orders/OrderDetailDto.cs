namespace marketplace_practice.Services.dto.Orders
{
    public class OrderDetailDto
    {
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Country { get; set; }
        public required string PostalCode { get; set; }
    }
}
