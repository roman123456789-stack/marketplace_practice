using marketplace_practice.Controllers.dto.Carts;
using marketplace_practice.Middlewares;
using marketplace_practice.Services;
using marketplace_practice.Services.dto.Carts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("carts")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Carts")]
    public class CartController : ControllerBase
    {
        private readonly CartService _cartService; // добавить интерфейс
        private readonly ILogger<CartController> _logger;

        public CartController(CartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateCartDto createCartDto)
        {
            try
            {
                var result = await _cartService.AddCartItemAsync(
                    User,
                    createCartDto.ProductId);

                //var result = await _cartService.AddCartItemAsync(
                //    User,
                //    createCartDto.ProductId,
                //    createCartDto.Quantity);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар успешно добавлен в корзину");
                    return StatusCode(201);
                }

                _logger.LogWarning("Ошибка при добавлении товара в корзину: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в корзину");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{targetUserId?}")]
        [Authorize]
        [ProducesResponseType(typeof(ICollection<CartItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string? targetUserId = null)
        {
            try
            {
                var result = await _cartService.GetCartAsync(User, targetUserId);

                if (result.IsSuccess)
                {
                    return Ok(new { Cart = result.Value?.Select(ci => new
                    {
                        CartItemId = ci.CartItemId,
                        Product = ci.productBriefInfo
                    }) });
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка товаров из корзины пользователя с ID = '{UserId}'",
                    User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{cartItemId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] string cartItemId)
        {
            try
            {
                var result = await _cartService.DeleteCartItemAsync(User, cartItemId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар c ID = '{CartId}' успешно удалён из корзины", cartItemId);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при удалении товара с ID = '{CartId}' из корзины: {Errors}",
                    cartItemId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении товара c ID = '{CartId}' из корзины", cartItemId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Товар не найден в корзине" => NotFound(new { Error = firstError }),
                "Не удалось идентифицировать пользователя"
                    => Unauthorized(new { Error = firstError }),
                "Отказано в доступе к корзине" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
