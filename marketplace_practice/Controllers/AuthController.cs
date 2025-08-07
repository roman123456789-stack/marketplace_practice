using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.dto;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                RegisterResultDto result = await _authService.RegisterAsync(dto);
                Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(10),
                    Path = "/"
                });
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            try
            {
                var result = await _authService.ConfirmEmailAsync(userId, token);

                if (result.Succeeded)
                    return Ok("Email успешно подтверждён");

                return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _authService.LoginAsync(loginDto);

            if (result.SignInResult.Succeeded)
            {
                if (result.AccessTokenResult != null && result.RefreshToken != null)
                {
                    Response.Cookies.Append("refreshToken",
                        result.RefreshToken,
                        new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true, // Только HTTPS
                            SameSite = SameSiteMode.Strict
                        });

                    return Ok(new { result.AccessTokenResult, result.RefreshToken });
                }
                else
                {
                    return BadRequest("Ошибка при входе");
                }
            }
            //if (result.SignInResult.RequiresTwoFactor)
            //{
            //    return RedirectToAction("LoginWith2fa");
            //}
            if (result.SignInResult.IsLockedOut)
            {
                return BadRequest(result.ErrorMessage);
            }

            return Unauthorized(result.ErrorMessage);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshToken)
        {
            var token = await _authService.RefreshTokenAsync(refreshToken);

            if (token != null)
            {
                Response.Cookies.Append("refreshToken",
                    token.RefreshToken,
                    new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true, // Только HTTPS
                        SameSite = SameSiteMode.Strict
                    });

                return Ok(token);
            }
            else
            {
                return BadRequest("Пользователь не аутентифицирован");
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
                await _authService.LogoutAsync(User);

                Response.Cookies.Delete("refreshToken", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                return Ok("Вы успешно вышли из системы");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("recovery")]
        [AllowAnonymous]
        public async Task<IActionResult> Recovery([FromBody] PasswordRecoveryDto request)
        {
            try
            {
                var result = await _authService.RecoveryAsync(request.Email);

                if (result.Success)
                {
                    return Ok(new
                    {
                        Message = "Ссылка для восстановления отправлена на email",
                        ResetToken = result.ResetToken
                    }); // <--- Только для тестов
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Ошибка восстановления пароля",
                    Detail = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Ошибка сервера",
                    Detail = "Произошла внутренняя ошибка при обработке запроса"
                });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
        {
            try
            {
                var result = await _authService.ResetPasswordAsync(request.Email, request.Token, request.NewPassword);

                if (result.Success)
                {
                    return Ok(new { Message = "Пароль успешно изменён" });
                }

                return BadRequest(new ProblemDetails
                {
                    Title = "Ошибка сброса пароля",
                    Detail = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ProblemDetails
                {
                    Title = "Ошибка сервера",
                    Detail = "Произошла внутренняя ошибка при обработке запроса"
                });
            }
        }
    }
}
