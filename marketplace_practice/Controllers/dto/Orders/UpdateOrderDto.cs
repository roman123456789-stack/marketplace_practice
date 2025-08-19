using marketplace_practice.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Orders
{
    public class UpdateOrderDto
    {
        public OrderStatus? OrderStatus { get; set; }
        public Dictionary<long, int>? CartItemQuantities { get; set; }


        //[StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        //public string? Type { get; set; }

        //[Range(1, long.MaxValue, ErrorMessage = "Количество баллов должно быть больше 0")]
        //public long? Points { get; set; }

        //public string? Description { get; set; }


        [StringLength(200, ErrorMessage = "Поле не может быть длиннее 200 символов")]
        public string? FullName { get; set; }

        [StringLength(50, ErrorMessage = "Поле не может быть длиннее 50 символов")]
        [Phone(ErrorMessage = "Номер телефона введен некорректно")]
        public string? PhoneNumber { get; set; }

        [StringLength(200, ErrorMessage = "Поле не может быть длиннее 200 символов")]
        public string? Country { get; set; }

        public string? PostalCode { get; set; }
    }
}
