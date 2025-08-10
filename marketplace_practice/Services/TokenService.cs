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

        public Token GenerateAccessToken(long userId, string email, string role, bool isVerified = false)
        {
            var tokenExpiration = TimeSpan.FromMinutes(double.Parse(_jwtConfig.Lifetime));
            var tokenExpirationDate = DateTime.UtcNow.Add(tokenExpiration);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim("is_verified", isVerified.ToString().ToLower(), ClaimValueTypes.Boolean)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtConfig.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtConfig.Issuer,
                audience: _jwtConfig.Audience,
                claims: claims,
                expires: tokenExpirationDate,
                signingCredentials: creds);

            return new Token
            {
                Value = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresAt = tokenExpirationDate
            };
        }

        public Token GenerateRefreshToken()
        {
            string refreshToken = Guid.NewGuid().ToString();
            DateTime expiresAt = DateTime.UtcNow.AddDays(30);
            return new Token
            {
                Value = refreshToken,
                ExpiresAt = expiresAt,
            };
        }
    }
}
