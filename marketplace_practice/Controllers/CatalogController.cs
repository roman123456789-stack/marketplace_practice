using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("catalog")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Catalog")]
    public class CatalogController : Controller
    {
        private readonly ICatalogService _catalogService;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
        {
            _catalogService = catalogService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize(Roles = "MainAdmin")]
        [ValidateModel]
        [ProducesResponseType(typeof(CategoryHierarchyDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CategoryHierarchyDto categoryHierarchy)
        {
            try
            {
                var result = await _catalogService.AddCategoryAsync(categoryHierarchy);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Категория успешно добавлена в каталог");
                    return StatusCode(201, result.Value);
                }

                _logger.LogWarning("Ошибка при добавлении категории в каталог: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении категории в каталог");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet]
        [ProducesResponseType(typeof(ICollection<CategoryHierarchyDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCategoryHierarchies()
        {

            try
            {
                var result = await _catalogService.GetCategoryHierarchiesAsync();

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении популярных товаров");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{*categoryPath}")]
        [ProducesResponseType(typeof(ICollection<ProductDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProductsByCategoryPath([FromRoute] string categoryPath)
        {
            var decodedPath = Uri.UnescapeDataString(categoryPath);
            var pathSegments = decodedPath.Split('/');

            try
            {
                var result = await _catalogService.GetProductsFromCategory(User, pathSegments);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении популярных товаров");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Категория не найдена" => NotFound(new { Error = firstError }),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
