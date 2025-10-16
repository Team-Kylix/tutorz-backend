using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;


namespace Tutorz.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        // We will inject a service here later

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            // Logic to register user
            return Ok(); // Placeholder
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            // Logic to log user in and generate token
            return Ok(); // Placeholder
        }
    }
}
