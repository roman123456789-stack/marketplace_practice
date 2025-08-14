using marketplace_practice.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Orders
{
    public class CreateOrderDto
    {
        [Required(ErrorMessage = "Поле 'ProductsId' не может быть пустым")]
        public Dictionary<long, int> productQuantities { get; set; }

        [Required(ErrorMessage = "Поле 'Currency' не может быть пустым")]
        [RegularExpression("^(RUB|USD|EUR|CNY)$", ErrorMessage = "Неверный формат валюты (допустимо: RUB, USD, EUR, CNY))")]
        public Currency Currency { get; set; }


        //[Required(ErrorMessage = "Поле 'Type' не может быть пустым")]
        //[StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        //public string Type { get; set; }

        //[Required(ErrorMessage = "Поле 'Points' не может быть пустым")]
        //[Range(1, long.MaxValue, ErrorMessage = "Количество баллов должно быть больше 0")]
        //public long Points { get; set; }

        //public string? Description { get; set; }


        [Required(ErrorMessage = "Поле 'FullName' не может быть пустым")]
        [StringLength(200, ErrorMessage = "Поле не может быть длиннее 200 символов")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Поле 'PhoneNumber' не может быть пустым")]
        [StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        [Phone(ErrorMessage = "Номер телефона введен некорректно")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Поле 'Country' не может быть пустым")]
        [StringLength(200, ErrorMessage = "Поле не может быть длиннее 200 символов")]
        public string Country { get; set; }

        [Required(ErrorMessage = "Поле 'PostalCode' не может быть пустым")]
        public string PostalCode { get; set; }
    }
}
