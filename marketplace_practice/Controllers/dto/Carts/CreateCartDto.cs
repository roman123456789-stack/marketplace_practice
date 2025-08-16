using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Carts
{
    public class CreateCartDto
    {
        [Required(ErrorMessage = "Поле 'ProductId' не может быть пустым")]
        public long ProductId { get; set; }

        //[Required(ErrorMessage = "Поле 'Quantity' не может быть пустым")]
        //[Range(1, int.MaxValue, ErrorMessage = "Количество должно быть больше 0")]
        //public int Quantity { get; set; }
    }
}
