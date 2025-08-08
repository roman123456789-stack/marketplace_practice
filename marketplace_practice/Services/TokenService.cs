using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace marketplace_practice.Services
{
    public class TokenService : ITokenService
    {
        private readonly JwtConfiguration _jwtConfig;

        public TokenService(IOptions<JwtConfiguration> jwtConfig)
        {
            _jwtConfig = jwtConfig.Value;
        }

        public AccessTokenResult GenerateAccessToken(long userId, string email, string role, bool is_verified = false)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("is_verified", is_verified.ToString().ToLower()),
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            string access_token = new JwtSecurityTokenHandler().WriteToken(token);
            DateTime expires_at = DateTime.UtcNow.AddDays(15);
            return new AccessTokenResult
            {
                Token = access_token,
                ExpiresAt = expires_at,
            };
        }
        public RefreshTokenModel GenerateRefreshToken()
        {
            string refreshToken = Guid.NewGuid().ToString();
            DateTime expiresAt = DateTime.UtcNow.AddDays(30);
            return new RefreshTokenModel(refreshToken, expiresAt);
        }
    }
}
