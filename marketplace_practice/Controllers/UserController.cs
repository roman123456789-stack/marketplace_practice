using marketplace_practice.Controllers.dto;
using marketplace_practice.Services.interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Get()
        {
            return Ok("Dev version");
        }

        [HttpPatch]
        [Authorize]
        public IActionResult Update()
        {
            return Ok("Dev version");
        }

        [HttpDelete]
        [Authorize]
        public IActionResult Delete()
        {
            return Ok("Dev version");
        }
    }
}
