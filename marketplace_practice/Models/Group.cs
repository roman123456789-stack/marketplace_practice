namespace marketplace_practice.Models
{
    public class Group
    {
        public short Id { get; set; }
        public short CategoryId { get; set; }
        public short SubcategoryId { get; set; }

        public Category Category { get; set; }
        public Subcategory Subcategory { get; set; }
        public ICollection<Product> Products { get; set; }
    }
}
