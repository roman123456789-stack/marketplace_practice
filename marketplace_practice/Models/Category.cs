namespace marketplace_practice.Models
{
    public class Category
    {
        public short Id { get; set; }
        public required string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; }

        public ICollection<Group> Groups { get; set; } = new List<Group>();
    }
}
