using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Identity;

namespace marketplace_practice.Services.dto
{
    public class AuthResultDto
    {
        public SignInResult SignInResult { get; set; }
        public AccessTokenResult AccessTokenResult { get; set; }
        public RefreshTokenModel RefreshToken { get; set; }
        public string ErrorMessage { get; set; }
    }
}
