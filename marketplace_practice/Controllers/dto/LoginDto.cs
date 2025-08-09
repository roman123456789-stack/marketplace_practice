using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class LoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [MinLength(8)]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
