using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("favoriteProducts")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Products")]
    public class FavoriteProductController : ControllerBase
    {
        private readonly IFavoriteProductService _favoriteProductService;
        private readonly IFavoriteCookieService _favoriteCookieService;
        private readonly ICartCookieService _cartCookieService;
        private readonly ILogger<FavoriteProductController> _logger;

        public FavoriteProductController(
            IFavoriteProductService favoriteProductService,
            IFavoriteCookieService favoriteCookieService,
            ICartCookieService cartCookieService,
            ILogger<FavoriteProductController> logger)
        {
            _favoriteProductService = favoriteProductService;
            _favoriteCookieService = favoriteCookieService;
            _cartCookieService = cartCookieService;
            _logger = logger;
        }

        /// <summary>
        /// Добавление товара в избранное
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromQuery] string productId)
        {
            try
            {
                if (User.Identity.IsAuthenticated)
                {
                    var result = await _favoriteProductService.AddToFavoritesAsync(User, productId);
                    return HandleAddFavoriteResult(result);
                }
                else
                {
                    var result = await _favoriteCookieService.AddToFavoritesCookieAsync(productId, HttpContext);
                    return HandleAddFavoriteResult(result);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в каталог");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleAddFavoriteResult(Result<string> result)
        {
            if (result.IsSuccess)
            {
                _logger.LogInformation("Товар успешно добавлен в избранное");
                return StatusCode(201);
            }

            _logger.LogWarning("Ошибка при добавлении товара в избранное: {Errors}",
                string.Join(", ", result.Errors));

            return HandleFailure(result.Errors);
        }

        /// <summary>
        /// Получение списка товаров из избранного
        /// </summary>
        [HttpGet]
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
                if (User.Identity.IsAuthenticated)
                {
                    var result = await _favoriteProductService.GetFavoritesAsync(User, userId);

                    if (result.IsSuccess)
                    {
                        return Ok(result.Value);
                    }

                    return HandleFailure(result.Errors);
                }
                else
                {
                    var favoriteItems = await _favoriteCookieService.GetFavoritesFromCookieAsync(HttpContext);

                    if (!favoriteItems.IsSuccess || !favoriteItems.Value.Any())
                    {
                        return Ok(Enumerable.Empty<ProductDto>());
                    }

                    var cartProductIds = (await _cartCookieService.GetCartFromCookieAsync(HttpContext))
                        .Value?
                        .Select(ci => ci.ProductId.ToString())
                        .ToList() ?? new List<string>();

                    var favoriteResult = await _favoriteProductService.GetFavoritesFromCookieAsync(
                        favoriteItems.Value,
                        cartProductIds);

                    return favoriteResult.IsSuccess
                        ? Ok(favoriteResult.Value)
                        : HandleFailure(favoriteResult.Errors);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении избранных товаров пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Удаление товара из избранного
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
                else
                {
                    await _favoriteCookieService.RemoveFromFavoritesCookieAsync(productId, HttpContext);
                    return NoContent();
                }
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
