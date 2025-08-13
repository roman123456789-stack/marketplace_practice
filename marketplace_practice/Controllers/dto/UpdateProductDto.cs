using marketplace_practice.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class UpdateProductDto
    {
        [StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public decimal? Price { get; set; }

        [RegularExpression("^(RUB|USD|EUR|CNY)$", ErrorMessage = "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY))")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Цена должна быть больше 0")]
        public Currency? Currency { get; set; }

        [StringLength(100, ErrorMessage = "Поле не может быть длиннее 100 символов")]
        public string? Category { get; set; }

        [StringLength(100, ErrorMessage = "Поле не может быть длиннее 100 символов")]
        public string? Subcategory { get; set; }

        public ICollection<string>? ImagesUrl { get; set; }
    }
}
