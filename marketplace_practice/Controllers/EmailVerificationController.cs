using marketplace_practice.Controllers.dto;
using marketplace_practice.Services;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("email-verification")]
    public class EmailVerificationController : Controller
    {
        private readonly UserService _userService;

        public EmailVerificationController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(
            [FromQuery] string userId,
            [FromQuery] string token)
        {
            try
            {
                var result = await _userService.ConfirmEmailAsync(userId, token);

                if (result.Succeeded)
                    return Ok("Email успешно подтверждён");

                return BadRequest(string.Join(", ", result.Errors.Select(e => e.Description)));
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
