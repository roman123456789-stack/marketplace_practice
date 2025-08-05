namespace marketplace_practice.Models
{
    public class OrderDetail
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Country { get; set; }
        public string PostalCode { get; set; }

        public Order Order { get; set; }
    }
}
