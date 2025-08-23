using marketplace_practice.Controllers.dto.Products;

namespace marketplace_practice.Services.dto.Catalog
{
    public class CategoryHierarchiesDto
    {
        public required string Name { get; set; }
        public ICollection<CategoryHierarchiesDto>? Children { get; set; }
    }
}
