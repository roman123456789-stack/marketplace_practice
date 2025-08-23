namespace marketplace_practice.Models
{
    public class ProductImage
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public short PriorityId { get; set; }
        public required string Url { get; set; }
        public bool IsMain { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public Product Product { get; set; }
    }
}
