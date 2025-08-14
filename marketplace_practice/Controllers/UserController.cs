using marketplace_practice.Controllers.dto.Users;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Users;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("users")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, ILogger<UserController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateModel]
        [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateUserDto createUserDto)
        {
            try
            {
                var result = await _userService.CreateUserAsync(
                    createUserDto.Email,
                    createUserDto.Password,
                    createUserDto.Role,
                    createUserDto.FirstName,
                    createUserDto.LastName);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Пользователь '{UserName}' успешно зарегистрирован в системе", createUserDto.Email);
                    return StatusCode(201, new { User = result.Value.Item1, EmailConfirmationToken = result.Value.Item2 });
                }

                _logger.LogWarning("Ошибка при регистрации пользователя '{UserName}' в системе: {Errors}",
                    createUserDto.Email, string.Join(", ", result.Errors));

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при регистрации пользователя");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{userId}")]
        [Authorize]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string userId)
        {
            try
            {
                var result = await _userService.GetUserByIdAsync(User, userId);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении данных пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPatch("{userId}")]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] string userId,
            [FromBody] UpdateUserDto updateUserDto)
        {
            try
            {
                var result = await _userService.UpdateUserAsync(
                    User,
                    userId,
                    updateUserDto.FirstName,
                    updateUserDto.LastName,
                    updateUserDto.PhoneNumber);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Данные пользователя c ID = '{UserId}' успешно обновлены", userId);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка при обновлении данных пользователя с ID = '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{userId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] string userId)
        {
            try
            {
                var result = await _userService.DeleteUserAsync(User, userId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Аккаунт пользователя c ID = '{UserId}' успешно удалён", userId);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при удалении аккаунта пользователя с ID = '{UserId}': {Errors}",
                    userId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении аккаунта пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Пользователь не найден" => NotFound(new { Error = firstError }),
                "Пользователь с таким email уже существует" 
                    => Conflict(new { Error = firstError }),
                "Недостаточно прав для выполнения действия" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
