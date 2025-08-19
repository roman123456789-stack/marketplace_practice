using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Products
{
    public class CategoryHierarchyDto
    {
        [Required(ErrorMessage = "Поле 'CategoryName' не может быть пустым")]
        [StringLength(100, ErrorMessage = "Поле не может быть длиннее 100 символов")]
        public string Name { get; set; }
        public CategoryHierarchyDto? Child { get; set; }
    }
}
