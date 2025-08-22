using marketplace_practice.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Products
{
    public class CreateProductDto
    {
        [Required(ErrorMessage = "Поле 'Name' не может быть пустым")]
        [StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        public string Name { get; set; }

        public string? Description { get; set; }

        [Required(ErrorMessage = "Поле 'Price' не может быть пустым")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public decimal PromotionalPrice { get; set; }

        [Range(1, short.MaxValue, ErrorMessage = "Размер должен быть больше 0")]
        public short Size { get; set; }

        [Required(ErrorMessage = "Поле 'Currency' не может быть пустым")]
        [RegularExpression("^(RUB|USD|EUR|CNY)$", ErrorMessage = "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY))")]
        public string Currency { get; set; } = "RUB";

        [Required(ErrorMessage = "Поле 'CategoryHierarchy' не может быть пустым")]
        public ICollection<CategoryHierarchyDto> CategoryHierarchy { get; set; }

        public ICollection<string>? ImagesUrl { get; set; }

        [Required(ErrorMessage = "Поле 'StockQuantity' не может быть пустым")]
        [Range(0, int.MaxValue, ErrorMessage = "Количество не может быть отрицательным")]
        public int StockQuantity { get; set; }
    }
}
