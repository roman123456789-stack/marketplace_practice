using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto.Auth
{
    public class ConfirmEmailChangeDto
    {
        [Required]
        public string UserId { get; set; }

        [Required]
        [EmailAddress]
        public string NewEmail { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
