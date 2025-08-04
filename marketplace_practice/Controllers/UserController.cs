using marketplace_practice.Controllers.dto;
using marketplace_practice.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace marketplace_practice.Controllers
{
    [ApiController]
    [Route("users")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        [Authorize]
        public IActionResult Get()
        {
            return Ok("Dev version");
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            CreateUserResultDto result = await _userService.CreateUserAsync(dto);
            return Ok(result);
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
