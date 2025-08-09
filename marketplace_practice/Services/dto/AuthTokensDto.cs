using marketplace_practice.Services.service_models;
using System.ComponentModel.DataAnnotations;

namespace marketplace_practice.Services.dto
{
    public class AuthTokensDto
    {
        [Required]
        public Token AccessToken { get; set; }

        [Required]
        public Token RefreshToken { get; set; }
    }
}
