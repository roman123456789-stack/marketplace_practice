using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Auth
{
    public class EmailDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }
    }
}
