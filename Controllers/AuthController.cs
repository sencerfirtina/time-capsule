using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TimeCapsule.API.DTO;
using TimeCapsule.API.Services;

namespace TimeCapsule.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO request)
        {
            var response = await _authService.RegisterAsync(request);

            if (!response.isSuccess)
            {
                return BadRequest(response.errorMessage);
            }

            return Ok("Registration completed successfully");
        }
    }
}