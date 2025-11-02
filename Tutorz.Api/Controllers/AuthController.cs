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

        // Inject the service
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
                // --- MODIFICATION ---
                // Log the inner exception to your console
                Console.WriteLine($"Error: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                }

                return BadRequest(new
                {
                    message = ex.Message,
                    // send the inner exception to the frontend for debugging
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                // Call the service
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
