using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace marketplace_practice.Utils
{
    public class JwtRefreshTokenHandler : AuthenticationHandler<JwtRefreshTokenOptions>
    {
        private readonly IAuthService _authService;
        private readonly JwtConfiguration _jwtConfig;

        public JwtRefreshTokenHandler(
            IOptionsMonitor<JwtRefreshTokenOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IAuthService authService,
            IOptions<JwtConfiguration> jwtConfig)
            : base(options, logger, encoder)
        {
            _authService = authService;
            _jwtConfig = jwtConfig.Value;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // 1. Проверяем access-токен из заголовка Authorization
            var accessToken = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(accessToken))
            {
                return AuthenticateResult.NoResult();
            }

            // 2. Если access-токен валиден — пропускаем запрос дальше
            var principal = ValidateAccessToken(accessToken);
            if (principal != null)
            {
                var ticket = new AuthenticationTicket(principal, Scheme.Name);
                return AuthenticateResult.Success(ticket);
            }

            // 3. Если access-токен невалиден, проверяем refresh-токен из куки
            var refreshToken = Request.Cookies["refreshToken"];
            if (string.IsNullOrEmpty(refreshToken))
            {
                return AuthenticateResult.Fail("Refresh-токен отсутствует");
            }

            // 4. Пытаемся обновить токены
            var refreshResult = await _authService.RefreshTokenAsync(refreshToken);
            if (!refreshResult.IsSuccess)
            {
                return AuthenticateResult.Fail("Не удалось обновить токен");
            }

            // 5. Обновляем куку с refresh-токеном
            Response.Cookies.Append("refreshToken", refreshResult.Value!.RefreshToken.Value,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

            // 6. Возвращаем новый access-токен в заголовке
            Response.Headers.Append("X-New-Access-Token", refreshResult.Value.AccessToken.Value);

            // 7. Создаем новый билет аутентификации
            var newPrincipal = ValidateAccessToken(refreshResult.Value.AccessToken.Value);

            if (newPrincipal != null)
            {
                var newTicket = new AuthenticationTicket(newPrincipal, Scheme.Name);
                return AuthenticateResult.Success(newTicket);
            }
            else
            {
                return AuthenticateResult.NoResult();
            }
        }

        private ClaimsPrincipal? ValidateAccessToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var validationParams = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = _jwtConfig.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtConfig.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(_jwtConfig.Key!)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParams, out _);
            }
            catch (SecurityTokenException ex)
            {
                return null;
            }
        }
    }

    public class JwtRefreshTokenOptions : AuthenticationSchemeOptions { }
}
