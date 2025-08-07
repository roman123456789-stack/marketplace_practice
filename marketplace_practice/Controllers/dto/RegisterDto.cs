using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Некорректный email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Пароль обязателен")]
        [MinLength(8, ErrorMessage = "Пароль должен быть не менее 8 символов")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Роль обязательна")]
        [RegularExpression("^(Покупатель|Продавец)$", ErrorMessage = "Роль может быть только 'Покупатель' или 'Продавец'")]
        public string Role { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
        public string? FirstName { get; set; }

        [StringLength(100, ErrorMessage = "Фамилия не может быть длиннее 100 символов")]
        public string? LastName { get; set; }
    }
}
