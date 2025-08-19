using marketplace_practice.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Products
{
    public class UpdateProductDto
    {
        [StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal? PromotionalPrice { get; set; }

        [Range(1, short.MaxValue, ErrorMessage = "Размер должен быть больше 0")]
        public short? Size { get; set; }

        [RegularExpression("^(RUB|USD|EUR|CNY)$", ErrorMessage = "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY))")]
        public Currency? Currency { get; set; }

        public ICollection<CategoryHierarchyDto>? CategoryHierarchy { get; set; }

        public ICollection<string>? ImagesUrl { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int? StockQuantity { get; set; }
    }
}
