using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("auth")]
    [ApiExplorerSettings(GroupName = "Auth")]
    public class TestAccessController : Controller
    {
        // ========================= ТЕСТОВЫЕ КОНТРОЛЛЕРЫ ================================

        [Authorize(Roles = "Продавец")]
        [HttpGet("authorize-test-seller")]
        public IActionResult LoginTestSeller()
        {
            return Ok("Пользователь аутентифицирован");
        }

        [Authorize(Roles = "Покупатель")]
        [HttpGet("authorize-test-buyer")]
        public IActionResult LoginTestBuyer()
        {
            return Ok("Пользователь аутентифицирован");
        }
    }
}
