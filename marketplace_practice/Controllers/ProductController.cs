using marketplace_practice.Controllers.dto.Products;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Products;
using marketplace_practice.Services.interfaces;
using marketplace_practice.Services.service_models;
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
        private readonly IFileUploadService _fileUploadService;
        private readonly IPDFService _pdfService;
        public ProductController(IProductService productsService, ILogger<ProductController> logger, IFileUploadService fileUploadService, IPDFService pdfService)
        {
            _productService = productsService;
            _logger = logger;
            _fileUploadService = fileUploadService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Создание товара
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateModel]
        [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] CreateProductDto createProductDto)
        {
            /*
                                СПРАВКА ПО РАБОТЕ С ФОРМОЙ

            0) Нужна роль "Admin" (можно искусственно ее добавить в таблице AspNetUserRoles)
            1) Все поля я сделал строковыми, а потом валидировал и парсил вручную, потому что asp.net вообще не умеет работать с формами
            2) В полях "Price", "PromotionalPrice", если значение дробное, то нужно писать его через запятую (НЕ точку!)
            3) Поле "Currency" принимает только "RUB", "USD", "EUR", "CNY". Все в верхнем регистре, но могу и нижний добавить, если надо.
            4) В поле "CategoryHierarchy" нужно указать путь в формате JSON (обязательно заключив в [квадратные скобки], т.к. это массив)
            5) Поля "Description", "PromotionalPrice", "Images" - необязательные
             */

            try
            {
                var validationResult = createProductDto.ValidateForm();

                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        Title = "Validation failed",
                        Status = 400,
                        Errors = validationResult.Errors
                    });
                }

                var result = await _productService.CreateProductAsync(
                    User,
                    createProductDto.Name,
                    createProductDto.Description,
                    createProductDto.GetPrice(),
                    createProductDto.GetPromotionalPrice(),
                    createProductDto.GetSize(),
                    createProductDto.Currency!,
                    createProductDto.GetCategoryHierarchy()!,
                    createProductDto.Images,
                    createProductDto.GetStockQuantity());

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

        /// <summary>
        /// Получение данных конкретного товара
        /// </summary>
        [HttpGet("{productId}")]
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
                var result = await _productService.GetProductByIdAsync(User, productId, HttpContext);

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

        /// <summary>
        /// Получение списка товаров конкретного пользователя
        /// </summary>
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

        /// <summary>
        /// Изменение данных товара
        /// </summary>
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

        /// <summary>
        /// Удаление товара
        /// </summary>
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


        // Тестовые эндпониты
        [HttpGet("generate")]
        public async Task<IActionResult> GeneratePdf()
        {
            try
            {
                // Создаём тестовые данные
                var receipt = new ReceiptModel
                {
                    ReceiptNumber = "INV-2025-001",
                    StoreName = "Marketplace Practice",
                    CustomerName = "Иван Иванов",
                    IssueDate = DateTime.Now,
                    Items = new List<ReceiptItem>
                    {
                        new() { ProductName = "Ноутбук", Quantity = 1, UnitPrice = 59990 },
                        new() { ProductName = "Мышь", Quantity = 2, UnitPrice = 1500 }
                    }
                };

                // Генерируем и сохраняем PDF
                var fileUrl = await _pdfService.SaveReceiptAsPdfAsync(receipt, "receipts");

                // Возвращаем JSON с ссылкой
                return Ok(new
                {
                    message = "PDF успешно сгенерирован",
                    url = fileUrl  // Например: /uploads/documents/abc123.pdf
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Ошибка генерации PDF", details = ex.Message });
            }
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadImages([FromForm] List<IFormFile> images)
        {
            if (images == null || images.Count == 0)
                return BadRequest("No files uploaded.");

            // Опционально: ограничение количества
            if (images.Count > 10)
                return BadRequest("Maximum 10 files allowed.");

            try
            {
                var urls = await _fileUploadService.SaveFilesAsync(images, "products");

                return Ok(new
                {
                    urls,
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while saving the files.");
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
