using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("products")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Products")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productsService, ILogger<ProductController> logger)
        {
            _productService = productsService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                var result = await _productService.CreateProductAsync(
                    User,
                    createProductDto.Name,
                    createProductDto.Description,
                    createProductDto.Price,
                    createProductDto.PromotionalPrice,
                    createProductDto.Size,
                    createProductDto.Currency,
                    createProductDto.CategoryHierarchy,
                    createProductDto.ImagesUrl,
                    createProductDto.StockQuantity);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар успешно добавлен в каталог");
                    return StatusCode(201, result.Value);
                }

                _logger.LogWarning("Ошибка при добавлении товара в каталог: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении товара в каталог");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{productId}")]
        [Authorize]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string productId)
        {
            try
            {
                var result = await _productService.GetProductByIdAsync(User, productId);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении товара c ID = '{ProductId}'", productId);
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
                var result = await _productService.GetProductListAsync(User, userId);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении товаров пользователя c ID = '{UserId}'", userId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPatch("{productId}")]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] string productId,
            [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                var result = await _productService.UpdateProductAsync(
                    User,
                    productId,
                    updateProductDto.Name,
                    updateProductDto.Description,
                    updateProductDto.Price,
                    updateProductDto.PromotionalPrice,
                    updateProductDto.Size,
                    updateProductDto.Currency,
                    updateProductDto.CategoryHierarchy,
                    updateProductDto.ImagesUrl,
                    updateProductDto.StockQuantity);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Данные товара c ID = '{ProductId}' успешно обновлены", productId);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка при обновлении данных товара с ID = '{ProductId}': {Errors}",
                    productId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных товара c ID = '{ProductId}'", productId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{productId}")]
        [Authorize(Roles = "Admin")]
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
                var result = await _productService.DeleteProductAsync(User, productId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Товар c ID = '{ProductId}' успешно удалён", productId);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при удалении товара с ID = '{ProductId}': {Errors}",
                    productId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении товара c ID = '{ProductId}'", productId);
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
                "Нельзя удалить товар, так как он используется в других записях"
                    => Conflict(new { Error = firstError }),
                "Отказано в доступе" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
