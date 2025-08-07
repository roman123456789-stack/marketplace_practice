using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class ChangeEmailDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; }
    }
}
