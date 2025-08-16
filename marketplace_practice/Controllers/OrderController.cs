using marketplace_practice.Controllers.dto.Orders;
using marketplace_practice.Middlewares;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("orders")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Orders")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ILogger<OrderController> _logger;

        public OrderController(IOrderService orderService, ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _logger = logger;
        }

        [HttpPost]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto createOrderDto)
        {
            try
            {
                var result = await _orderService.CreateOrderAsync(
                    User,
                    createOrderDto.productQuantities,
                    createOrderDto.Currency,
                    createOrderDto.FullName,
                    createOrderDto.PhoneNumber,
                    createOrderDto.Country,
                    createOrderDto.PostalCode);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Заказ успешно создан");
                    return StatusCode(201, result.Value);
                }

                _logger.LogWarning("Ошибка при создании заказа: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании заказа");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpGet("{orderId}")]
        [Authorize]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] string orderId)
        {
            try
            {
                var result = await _orderService.GetOrderByIdAsync(User, orderId);

                if (result.IsSuccess)
                {
                    return Ok(result.Value);
                }

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении заказа c ID = '{OrderId}'", orderId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpPatch("{orderId}")]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update(
            [FromRoute] string orderId,
            [FromBody] UpdateOrderDto updateOrderDto)
        {
            try
            {
                var result = await _orderService.UpdateOrderAsync(
                    User,
                    orderId,
                    updateOrderDto.OrderStatus,
                    updateOrderDto.productQuantities,
                    updateOrderDto.FullName,
                    updateOrderDto.PhoneNumber,
                    updateOrderDto.Country,
                    updateOrderDto.PostalCode);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Данные заказа c ID = '{OrderId}' успешно обновлены", orderId);
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка при обновлении данных заказа с ID = '{OrderId}': {Errors}",
                    orderId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении данных заказа c ID = '{OrderId}'", orderId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        [HttpDelete("{orderId}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] string orderId)
        {
            try
            {
                var result = await _orderService.DeleteOrderAsync(User, orderId);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Заказ c ID = '{OrderId}' успешно удалён", orderId);
                    return NoContent();
                }

                _logger.LogWarning("Ошибка при удалении заказа с ID = '{OrderId}': {Errors}",
                    orderId, string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении заказа c ID = '{OrderId}'", orderId);
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Заказ не найден" => NotFound(new { Error = firstError }),
                "Не удалось идентифицировать пользователя"
                    => Unauthorized(new { Error = firstError }),
                "Нельзя удалить заказ, так как он используется в других записях"
                    => Conflict(new { Error = firstError }),
                "Нет доступа к данному заказу" => Forbid(),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
