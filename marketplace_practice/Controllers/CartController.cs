using marketplace_practice.Middlewares;
using marketplace_practice.Services;
using marketplace_practice.Services.dto.Carts;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("cart")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Cart")]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;
        private readonly ICartCookieService _cartCookieService;
        private readonly IFavoriteCookieService _favoriteCookieService;
        private readonly ILogger<CartController> _logger;

        public CartController(
            ICartService cartService,
            ICartCookieService cartCookieService,
            IFavoriteCookieService favoriteCookieService,
            ILogger<CartController> logger)
        {
            _cartService = cartService;
            _cartCookieService = cartCookieService;
            _favoriteCookieService = favoriteCookieService;
            _logger = logger;
        }

        /// <summary>
        /// Добавление товара в корзину
        /// </summary>
        [HttpPost]
        [ValidateModel]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddCartItem(
            [FromQuery] string productId,
            [FromQuery] int quantity = 1)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    // Для авторизованных - сохранение в БД
                    var result = await _cartService.AddCartItemAsync(User, productId, quantity);
                    return HandleAddCartItemResult(result);
                }
                else
                {
                    // Для неавторизованных - сохранение в куки
                    var result = await _cartCookieService.AddToCartCookieAsync(productId, quantity, HttpContext);
                    return HandleAddCartItemResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в корзину");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleAddCartItemResult(Result<string> result)
        {
            if (result.IsSuccess)
            {
                _logger.LogInformation("Товар успешно добавлен в корзину");
                return StatusCode(201);
            }

            _logger.LogWarning("Ошибка при добавлении товара в корзину: {Errors}",
                string.Join(", ", result.Errors));

            return HandleFailure(result.Errors);
        }

        /// <summary>
        /// Получение списка товаров из корзины
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ICollection<CartItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCart([FromQuery] string? userId = null)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var result = await _cartService.GetCartAsync(User, userId);

                    if (result.IsSuccess)
                    {
                        return Ok(new { result.Value });
                    }

                    return HandleFailure(result.Errors);
                }
                else
                {
                    var cartItems = await _cartCookieService.GetCartFromCookieAsync(HttpContext);

                    if (!cartItems.IsSuccess || !cartItems.Value.Any())
                    {
                        return Ok(Enumerable.Empty<CartItemDto>());
                    }

                    var favoriteIdsResult = await _favoriteCookieService.GetFavoritesFromCookieAsync(HttpContext);
                    var favoriteIds = favoriteIdsResult.IsSuccess ? favoriteIdsResult.Value : new List<string>();

                    var cartResult = await _cartService.GetCartFromCookieAsync(cartItems.Value, favoriteIds);

                    return cartResult.IsSuccess
                        ? Ok(cartResult.Value)
                        : HandleFailure(cartResult.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении корзины");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удаление товара из корзины
        /// </summary>
        [HttpDelete("{productId}")]
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
                if (User.Identity.IsAuthenticated)
                {
                    var result = await _cartService.DeleteCartItemAsync(User, productId);

                    if (result.IsSuccess)
                    {
                        _logger.LogInformation("Товар c ID = '{ProductId}' успешно удалён из корзины", productId);
                        return NoContent();
                    }

                    _logger.LogWarning("Ошибка при удалении товара с ID = '{ProductId}' из корзины: {Errors}",
                        productId, string.Join(", ", result.Errors));

                    return HandleFailure(result.Errors);
                }
                else
                {
                    await _cartCookieService.RemoveFromCartCookieAsync(productId, HttpContext);
                    return NoContent();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении товара c ID = '{ProductId}' из корзины", productId);
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
                "Отказано в доступе" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
