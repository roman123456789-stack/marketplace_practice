using marketplace_practice.Services.dto;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace marketplace_practice.Services
{
    public class TokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config)
        {
            _config = config;
        }

        public AccessTokenResult GenerateAccessToken(long userId, string email, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _config["Jwt:Key"]));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            string access_token = new JwtSecurityTokenHandler().WriteToken(token);
            DateTime expires_at = DateTime.UtcNow.AddDays(15);
            return new AccessTokenResult
            {
                AccessToken = access_token,
                ExpiresAt = expires_at,
            };
        }
        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString();
        }
    }
}
