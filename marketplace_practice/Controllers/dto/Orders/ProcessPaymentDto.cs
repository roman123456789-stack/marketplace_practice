using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Orders
{
    public class ProcessPaymentDto
    {
        [Required(ErrorMessage = "Поле 'OrderId' не может быть пустым")]
        public string OrderId { get; set; }

        [Required(ErrorMessage = "Поле 'ProviderName' не может быть пустым")]
        public required string ProviderName { get; set; }

        [Required(ErrorMessage = "Поле 'Amount' не может быть пустым")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Итоговая сумма должна быть больше 0")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Поле 'Currency' не может быть пустым")]
        [RegularExpression("^(RUB|USD|EUR|CNY)$", ErrorMessage = "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY))")]
        public required string Currency { get; set; }
    }
}
