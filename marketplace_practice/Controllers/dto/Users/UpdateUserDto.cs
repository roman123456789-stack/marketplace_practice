using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Users
{
    public class UpdateUserDto
    {
        [StringLength(100, ErrorMessage = "Имя не может быть длиннее 100 символов")]
        public string? FirstName { get; set; }
        [StringLength(100, ErrorMessage = "Фамилия не может быть длиннее 100 символов")]
        public string? LastName { get; set; }
        [Phone(ErrorMessage = "Номер телефона введен некорректно")]
        public string? PhoneNumber { get; set; }
    }
}
