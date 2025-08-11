using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Models
{
    public class Role : IdentityRole<long>
    {
        public string? Description { get; set; }
    }
}
