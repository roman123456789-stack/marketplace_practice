using marketplace_practice.Controllers.dto.Auth;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Auth;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("auth")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet("confirm-email-and-sign-in")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                            Secure = true,
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(7),
                            Path = "/"
                        });

                    _logger.LogInformation("Email подтверждён и пользователь с ID = '{UserId}' вошёл в систему", userId);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка подтверждения email пользователя с ID = '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors));


                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка подтверждения email пользователя с ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [ValidateModel]
        [ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                            SameSite = SameSiteMode.Strict,
                            Expires = DateTime.UtcNow.AddDays(7),
                            Path = "/"
                        });

                    _logger.LogInformation("Пользователь '{UserName}' успешно вошёл в систему", loginDto.Email);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка при входе пользователя '{UserName}' в систему: {Errors}",
                    loginDto.Email, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    "Аккаунт временно заблокирован" => StatusCode(403, new { Error = firstError }),
                    _ => Unauthorized(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при входе пользователя '{UserName}' в систему", loginDto.Email);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        //[HttpPost("refresh-token")]
        //[AllowAnonymous]
        //[ValidateModel]
        //[ProducesResponseType(typeof(AuthTokensDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status401Unauthorized)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        //{
        //    try
        //    {
        //        var result = await _authService.RefreshTokenAsync(refreshTokenDto.RefreshToken);

        //        if (result.IsSuccess)
        //        {
        //            Response.Cookies.Append("refreshToken",
        //                result.Value!.RefreshToken.Value,
        //                new CookieOptions
        //                {
        //                    HttpOnly = true,
        //                    Secure = true, // Только HTTPS
        //                    SameSite = SameSiteMode.Strict,
        //                    Expires = DateTime.UtcNow.AddDays(7),
        //                    Path = "/"
        //                });

        //            return Ok(result.Value);
        //        }

        //        _logger.LogWarning("Ошибка при обновлении токена доступа: {Errors}",
        //            string.Join(", ", result.Errors));

        //        var firstError = result.Errors.FirstOrDefault();
        //        return firstError switch
        //        {
        //            "Пользователь не найден" => NotFound(new { Error = firstError }),
        //            "Время жизни токена истекло" => Unauthorized(),
        //            _ => BadRequest(new { Errors = result.Errors })
        //        };
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Ошибка при обновлении токена");
        //        return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
        //    }
        //}

        [HttpPost("logout")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

                    _logger.LogInformation("Пользователь '{UserName}' успешно вышел из системы", User.Identity?.Name);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при выходе пользователя '{UserName}' из системы: {Errors}",
                    User.Identity?.Name, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выходе пользователя '{UserName}' из системы", User.Identity?.Name);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("recovery")]
        [AllowAnonymous]
        [ValidateModel]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Recovery([FromBody] EmailDto emailDto)
        {
            try
            {
                var result = await _authService.RecoveryAsync(emailDto.Email);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Запрос на сброс пароля пользователя '{UserName}' успешно отправлен на электронную почту",
                        emailDto.Email);

                    return Ok(new
                    {
                        ResetToken = result.Value  // <--- Только для тестов
                    });
                }

                _logger.LogWarning("Ошибка при сбросе пароля пользователя '{UserName}': {Errors}",
                    emailDto.Email, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при сбросе пароля пользователя '{UserName}'", emailDto.Email);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        [ValidateModel]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                    _logger.LogInformation("Пароль пользователя '{UserName}' успешно изменён", resetPasswordDto.Email);
                    return Ok(new { Success = true, Message = "Пароль успешно изменён" });
                }

                _logger.LogWarning("Ошибка при обновлении пароля пользователя '{UserName}': {Errors}",
                    resetPasswordDto.Email, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении пароля пользователя '{UserName}'", resetPasswordDto.Email);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPost("change-email")]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangeEmail([FromBody] EmailDto emailDto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var result = await _authService.InitiateEmailChangeAsync(userId, emailDto.Email);

                if (result.IsSuccess)
                {
                    _logger.LogInformation(
                        "Запрос на обновление электронной почты пользователя '{UserName}' успешно отправлен на новую электронную почту",
                        User.Identity?.Name);

                    return Ok(new
                    {
                        Token = result.Value  // <--- Только для тестов
                    });
                }

                _logger.LogWarning("Ошибка при изменении email пользователя '{UserName}': {Errors}",
                    User.Identity?.Name, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    "Email уже используется" => Conflict(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении email пользователя '{UserName}'", User.Identity?.Name);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("confirm-email-change")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                    _logger.LogInformation("Пароль пользователя с ID = '{UserId}' успешно изменён", userId);
                    return Ok(new { Success = true, Message = "Email успешно изменён" });
                }

                _logger.LogWarning("Ошибка при изменении email пользователя с ID = '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors));

                var firstError = result.Errors.FirstOrDefault();
                return firstError switch
                {
                    "Пользователь не найден" => NotFound(new { Error = firstError }),
                    _ => BadRequest(new { Errors = result.Errors })
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при изменении email пользователя с ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }
    }
}
