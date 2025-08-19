using marketplace_practice.Services;
using marketplace_practice.Services.dto.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("favoriteProducts")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "FavoriteProducts")]
    public class FavoriteProductController : ControllerBase
    {
        private readonly FavoriteProductService _favoriteProductService; // добавить интерфейс
        private readonly ILogger<FavoriteProductController> _logger;

        public FavoriteProductController(
            FavoriteProductService favoriteProductService,
            ILogger<FavoriteProductController> logger)
        {
            _favoriteProductService = favoriteProductService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromQuery] string productId)
        {
            try
            {
                var result = await _favoriteProductService.AddToFavoritesAsync(User, productId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар успешно добавлен в избранное");
                    return StatusCode(201);
                }

                _logger.LogWarning("Ошибка при добавлении товара в избранное: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в каталог");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet]
        [Authorize]
        [ProducesResponseType(typeof(ICollection<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductList([FromQuery] string? userId = null)
        {
            try
            {
                var result = await _favoriteProductService.GetFavoritesAsync(User, userId);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении избранных товаров пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{productId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] string productId)
        {
            try
            {
                var result = await _favoriteProductService.RemoveFromFavoritesAsync(User, productId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар c ID = '{ProductId}' успешно удалён из избранных", productId);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при удалении из избранных товара с ID = '{ProductId}': {Errors}",
                    productId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении из избранных товара c ID = '{ProductId}'", productId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Товар не найден" => NotFound(new { Error = firstError }),
                "Не удалось идентифицировать пользователя"
                    => Unauthorized(new { Error = firstError }),
                "Отказано в доступе" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
