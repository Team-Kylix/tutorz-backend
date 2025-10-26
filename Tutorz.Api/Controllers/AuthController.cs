using Microsoft.AspNetCore.Mvc;
using Tutorz.Application.DTOs.Auth; // DTOs
using Tutorz.Application.Interfaces; // Service Interface


namespace Tutorz.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase // Use ControllerBase for APIs
    {
        private readonly IAuthService _authService;

        // 1. Inject the service
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            try
            {
                var response = await _authService.RegisterAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                // 3. Call the service
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin(SocialLoginRequest request)
        {
            try
            {
                // Log the incoming request for debugging
                Console.WriteLine($"Social login request - Provider: {request.Provider}, Role: {request.Role}");

                var response = await _authService.SocialLoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                // Log the full error
                Console.WriteLine($"Social login error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                return BadRequest(new { message = ex.Message, details = ex.InnerException?.Message });
            }
        }
    }
}
