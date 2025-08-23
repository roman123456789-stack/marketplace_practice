using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Products
{
    public class CategoryHierarchyDto
    {
        [Required(ErrorMessage = "Поле 'CategoryName' не может быть пустым")]
        [StringLength(100, ErrorMessage = "Поле не может быть длиннее 100 символов")]
        public string Name { get; set; }
        public CategoryHierarchyDto? Child { get; set; }

        public List<string> Validate()
        {
            var errors = new List<string>();

            if (string.IsNullOrEmpty(Name))
            {
                errors.Add("Поле 'CategoryName' не может быть пустым");
            }
            else if (Name.Length > 100)
            {
                errors.Add("Поле не может быть длиннее 100 символов");
            }

            if (Child != null)
            {
                errors.AddRange(Child.Validate());
            }

            return errors;
        }
    }
}
