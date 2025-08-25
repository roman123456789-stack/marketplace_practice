using marketplace_practice.Controllers.dto.Orders;
using marketplace_practice.Middlewares;
using marketplace_practice.Services;
using marketplace_practice.Services.dto.Orders;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("payments")]
    [Produces("application/json")]
    [ApiExplorerSettings(GroupName = "Orders")]
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        /// <summary>
        /// Оплата заказа
        /// </summary>
        [HttpPost("process")]
        [Authorize]
        [ValidateModel]
        [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ProcessPayment([FromBody] ProcessPaymentDto processPaymentDto)
        {
            try
            {
                var result = await _paymentService.ProcessPaymentAsync(
                    processPaymentDto.OrderId,
                    processPaymentDto.ProviderName,
                    processPaymentDto.Amount,
                    processPaymentDto.Currency);

                if (result.IsSuccess)
                {
                    _logger.LogInformation("Оплата заказа прошла успешно");
                    return Ok(result.Value);
                }

                _logger.LogWarning("Ошибка при оплате заказа: {Errors}",
                    string.Join(", ", result.Errors));

                return HandleFailure(result.Errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при оплате заказа");
                return StatusCode(500, new { Error = "Внутренняя ошибка сервера" });
            }
        }

        private IActionResult HandleFailure(IEnumerable<string> errors)
        {
            var firstError = errors.FirstOrDefault();
            return firstError switch
            {
                "Заказ уже оплачен" => StatusCode(422, new { Error = firstError }),
                "Ошибка при создании чека" => StatusCode(422, new { Error = firstError }),
                "Заказ не найден" => NotFound(new { Error = firstError }),
                "Недостаточно товара на складе" => Conflict(new { Errors = errors }),
                _ => BadRequest(new { Errors = errors })
            };
        }
    }
}
