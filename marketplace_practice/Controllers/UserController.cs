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
        public IActionResult Create([FromBody] CreateUserDto dto)
        {
            CreateUserResultDto result = _userService.CreateUser(dto);
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
