using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class PasswordRecoveryDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
