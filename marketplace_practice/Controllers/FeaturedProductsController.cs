using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.dto.Products;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("featuredProducts")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Products")]
    public class FeaturedProductsController : ControllerBase
    {
        private readonly IFeaturedProductsService _featuredProductService;
        private readonly ILogger<FeaturedProductsController> _logger;

        public FeaturedProductsController(
            IFeaturedProductsService featuredProductService,
            ILogger<FeaturedProductsController> logger)
        {
            _featuredProductService = featuredProductService;
            _logger = logger;
        }

        /// <summary>
        /// Получение списка популярных товаров
        /// </summary>
        [HttpGet("popular")]
        [ProducesResponseType(typeof(ICollection<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPopularProductList([FromQuery] int limit = 4)
        {
            try
            {
                var result = await _featuredProductService.GetPopularProductsAsync(User, limit);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении популярных товаров");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        /// <summary>
        /// Получение списка новинок
        /// </summary>
        [HttpGet("new")]
        [ProducesResponseType(typeof(ICollection<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetNewProductList(
            [FromQuery] int limit = 4,
            [FromQuery] int days = 30)
        {
            try
            {
                var result = await _featuredProductService.GetNewProductsAsync(User, limit, days);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return BadRequest(new { Errors = result.Errors });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении новых товаров");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }
    }
}
