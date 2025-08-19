namespace marketplace_practice.Models
{
    public class Category
    {
        public short Id { get; set; }
        public short? ParentCategoryId { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
        public ICollection<Category> Subcategories { get; set; } = new List<Category>();
        public Category? ParentCategory { get; set; }
    }
}
