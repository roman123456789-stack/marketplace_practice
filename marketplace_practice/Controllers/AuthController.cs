using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                var result = await _authService.RegisterAsync(
                    registerDto.Email,
                    registerDto.Password,
                    registerDto.Role,
                    registerDto.FirstName,
                    registerDto.LastName);

                // Я думаю, при регистрации токены не нужны,
                // так как для полноценного доступа нужно еще подтвердить почту

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Пользователь успешно зарегистрировать в системе: {UserName}", registerDto.Email);
                    return Ok(new
                    {
                        User = result.Value!.User,
                        EmailVerificationToken = result.Value!.emailVerificationToken
                    });
                }

                _logger.LogWarning("Ошибка при регистрации пользователя в системе: {Errors}",
                    string.Join(", ", result.Errors));

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя в системе");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                return BadRequest(new { Error = "Неверные параметры запроса" });
            }

            try
            {
                var result = await _authService.ConfirmEmailAndSignInAsync(userId, token);

                if (result.IsSuccess)
                {
                    Response.Cookies.Append("refreshToken",
                        result.Value!.RefreshToken.Value,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = false,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(10),
                            Path = "/"
                        });

                    _logger.LogInformation("Email подтверждён и пользователь вошёл в систему: {UserId}", userId);
                    return Ok(new
                    {
                        AccessToken = result.Value!.AccessToken,
                        RefreshToken = result.Value!.RefreshToken
                    });
                }

                _logger.LogWarning("Ошибка подтверждения email: {UserId} - {Errors}",
                    userId, string.Join(", ", result.Errors));

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения email: {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var result = await _authService.LoginAsync(loginDto.Email, loginDto.Password, loginDto.RememberMe);

                if (result.IsSuccess)
                {
                    Response.Cookies.Append("refreshToken",
                        result.Value!.RefreshToken.Value,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true, // Только HTTPS
                            SameSite = SameSiteMode.Strict
                        });

                    _logger.LogInformation("Пользователь успешно вошёл в систему: {UserName}", loginDto.Email);
                    return Ok(new
                    {
                        AccessToken = result.Value!.AccessToken,
                        RefreshToken = result.Value!.RefreshToken
                    });
                }

                _logger.LogWarning("Ошибка при входе в аккаунт: {UserName} - {Errors}",
                    loginDto.Email, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Аккаунт временно заблокирован" => StatusCode(
                        StatusCodes.Status403Forbidden,
                        new { Error = firstError }),

                    //"Требуется двухфакторная аутентификация" => StatusCode(
                    //    StatusCodes.Status402PaymentRequired,
                    //    new { Error = firstError }),

                    _ => Unauthorized(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя {UserName}", loginDto.Email);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

                if (result.IsSuccess)
                {
                    Response.Cookies.Append("refreshToken",
                        result.Value!.RefreshToken.Value,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true, // Только HTTPS
                            SameSite = SameSiteMode.Strict
                        });

                    return Ok(new
                    {
                        AccessToken = result.Value!.AccessToken,
                        RefreshToken = result.Value!.RefreshToken
                    });
                }
                else
                {
                    return BadRequest("Пользователь не аутентифицирован");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении токена");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        // ================================= ТЕСТОВЫЕ КОНТРОЛЛЕРЫ =======================================

        [Authorize(Roles = "Продавец")]
        [HttpGet("authorize-test-seller")]
        public IActionResult LoginTestSeller()
        {
            return Ok("Пользователь аутентифицирован");
        }

        [Authorize(Roles = "Покупатель")]
        [HttpGet("authorize-test-buyer")]
        public IActionResult LoginTestBuyer()
        {
            return Ok("Пользователь аутентифицирован");
        }

        // ==============================================================================================

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var result = await _authService.LogoutAsync(User);

                if (result.IsSuccess)
                {
                    Response.Cookies.Delete("refreshToken", new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict
                    });

                    return Ok("Вы успешно вышли из системы");
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе из системы");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("recovery")]
        [AllowAnonymous]
        public async Task<IActionResult> Recovery([FromBody] EmailDto emailDto)
        {
            try
            {
                var result = await _authService.RecoveryAsync(emailDto.Email);

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        ResetToken = result.Value  // <--- Только для тестов
                    });
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сбросе пароля");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(
                    resetPasswordDto.Email,
                    resetPasswordDto.Token,
                    resetPasswordDto.NewPassword);

                if (result.IsSuccess)
                {
                    return Ok(new { Message = "Пароль успешно изменён" });
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сбросе пароля");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("change-email")]
        [Authorize]
        public async Task<IActionResult> ChangeEmail([FromBody] EmailDto emailDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _authService.InitiateEmailChangeAsync(userId, emailDto.Email);

                if (result.IsSuccess)
                {
                    return Ok(new
                    {
                        Token = result.Value  // <--- Только для тестов
                    });
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении email");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("confirm-email-change")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmailChange(
            [FromQuery] string userId,
            [FromQuery] string newEmail,
            [FromQuery] string token)
        {
            try
            {
                var result = await _authService.ConfirmEmailChangeAsync(userId, newEmail, token);

                if (result.IsSuccess)
                {
                    return Ok(new { Message = "Email успешно изменён" });
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении email");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Error = "Внутренняя ошибка сервера" });
            }
        }
    }
}
