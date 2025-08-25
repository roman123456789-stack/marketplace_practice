using marketplace_practice.Services;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("admin")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Admin")]
    [Authorize(Roles = "MainAdmin")]
    public class AdminController : Controller
    {
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger;
        }

        /// <summary>
        /// Выдача роли администратора конкретному пользователю
        /// </summary>
        [HttpGet("giveAdminRole")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GiveAdminRole(
            [FromQuery] string userId)
        {
            try
            {
                var result = await _adminService.GiveAdminRoleAsync(User, userId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Пользователь с ID = '{UserId}' успешно получил роль 'Admin'", userId);
                    return Ok(new { Success = true, Message = $"Пользователь с ID = '{userId}' успешно получил роль 'Admin'" });
                }

                _logger.LogWarning("Ошибка при выдачи роли 'Admin': {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при выдачи роли 'Admin'");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Пользователь не найден" => NotFound(new { Error = firstError }),
                "Не удалось идентифицировать пользователя"
                    => Unauthorized(new { Error = firstError }),
                "Отказано в доступе" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
