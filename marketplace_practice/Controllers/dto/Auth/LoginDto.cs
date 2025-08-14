using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Auth
{
    public class LoginDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}
