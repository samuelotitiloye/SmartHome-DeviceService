using Microsoft.AspNetCore.Mvc;
using DeviceService.Api.Auth;

namespace DeviceService.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
     public class AuthController : ControllerBase
     {
        private readonly JwtTokenService _tokenService;

        public AuthController(JwtTokenService tokenService)
        {
            _tokenService = tokenService;
        }

        /// <summary>
        /// Authenticates a user and returns a JWT token.
        /// </summary>
        /// <param name="request">User login credentials.</param>
        /// <returns>A JWT bearer token if authentication succeeds.</returns>
        /// <response code="200">JWT token successfully generated.</response>
        /// <response code="401">Invalid username or password.</response>
        [HttpPost("login")] // lowercased for consistent API design
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            //simple mock authentication
            if(request.Username != "admin" || request.Password != "password123")
                return Unauthorized(new { error = "Invalid credentials" });

              var token = _tokenService.GenerateToken(request.Username);

            return Ok(new { token });
        }
     }
}