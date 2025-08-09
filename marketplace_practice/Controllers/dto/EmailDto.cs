using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Controllers.dto
{
    public class EmailDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
