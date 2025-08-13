using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class RefreshTokenDto
    {
        [Required(ErrorMessage = "Поле должно быть заполнено")]
        public string RefreshToken { get; set; }
    }
}
