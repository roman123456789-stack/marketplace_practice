using marketplace_practice.Controllers.dto;
using marketplace_practice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        //[HttpGet]
        //[Authorize]
        //public IActionResult Get()
        //{
        //    return Ok("Dev version");
        //}

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
        {
            try
            {
                CreateUserResultDto result = await _userService.CreateUserAsync(dto);
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

        //[HttpPatch]
        //[Authorize]
        //public IActionResult Update()
        //{
        //    return Ok("Dev version");
        //}

        //[HttpDelete]
        //[Authorize]
        //public IActionResult Delete()
        //{
        //    return Ok("Dev version");
        //}

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            var result = await _userService.LoginAsync(loginDto);

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
            var token = await _userService.RefreshTokenAsync(refreshToken);

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

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _userService.LogoutAsync(User);

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

        [Authorize(Roles = "Продавец")]
        [HttpGet("login-test-seller")]
        public IActionResult LoginTestSeller()
        {
            return Ok("Пользователь аутентифицирован");
        }

        [Authorize(Roles = "Покупатель")]
        [HttpGet("login-test-buyer")]
        public IActionResult LoginTestBuyer()
        {
            return Ok("Пользователь аутентифицирован");
        }
    }
}
