using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Поле должно быть заполнено")]
        public string Token { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
        public string NewPassword { get; set; }
    }
}
