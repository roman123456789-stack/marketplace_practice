namespace marketplace_practice.Models
{
    public class OrderDetail
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public required string FullName { get; set; }
        public required string PhoneNumber { get; set; }
        public required string Country { get; set; }
        public required string PostalCode { get; set; }

        public Order Order { get; set; }
    }
}
