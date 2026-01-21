using Microsoft.AspNetCore.Mvc;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Application.Interfaces;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;

namespace Tutorz.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        // Constructor
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
                Console.WriteLine($"Error: {ex.Message}");
                return BadRequest(new
                {
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            try
            {
                var response = await _authService.LoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- CHECK USER STATUS ---
        [HttpPost("check-status")]
        public async Task<IActionResult> CheckStatus([FromBody] CheckUserRequest request)
        {
            var result = await _authService.CheckUserStatusAsync(request);

            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpPost("send-otp")]
        public async Task<IActionResult> SendOtp([FromBody] CheckUserRequest request)
        {
            try
            {
                // Call the REAL service now
                await _authService.SendOtpAsync(request.Identifier);
                return Ok(new { message = "OTP sent successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyUserRequest request)
        {
            try
            {
                // Return the object containing the phone number
                var result = await _authService.VerifyOtpAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- REGISTER SIBLING ---
        // Only called after OTP is verified
        [HttpPost("register-sibling")]
        public async Task<IActionResult> RegisterSibling([FromBody] SiblingRegistrationRequest request)
        {
            try
            {
                // Logic to add a new student to the existing parent account
                var response = await _authService.RegisterSiblingAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- SWITCH PROFILE (For Dashboard) ---
        // Allows a logged-in parent to get a new token for a different child
        [HttpPost("switch-profile")]
        public async Task<IActionResult> SwitchProfile([FromBody] SwitchProfileRequest request)
        {
            try
            {
                // Get current Parent User ID from the valid Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                // Fallback for standard JWT sub claim
                var subClaim = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) && string.IsNullOrEmpty(subClaim))
                {
                    return Unauthorized("Invalid token claims.");
                }

                var userId = Guid.Parse(userIdClaim ?? subClaim);

                // Call Service to validate that this StudentId belongs to this UserId
                // and generate a new token
                var response = await _authService.SwitchProfileAsync(userId, request.StudentId);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // --- SOCIAL LOGIN ---
        [HttpPost("social-login")]
        public async Task<IActionResult> SocialLogin(SocialLoginRequest request)
        {
            try
            {
                Console.WriteLine($"Social login request - Provider: {request.Provider}, Role: {request.Role}");
                var response = await _authService.SocialLoginAsync(request);
                return Ok(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Social login error: {ex.Message}");
                return BadRequest(new { message = ex.Message, details = ex.InnerException?.Message });
            }
        }

        // --- UTILS (Check Email, Forgot Pass) ---
        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            try
            {
                bool exists = await _authService.CheckEmailExistsAsync(email);
                return Ok(new { exists });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            try
            {
                await _authService.ForgotPasswordAsync(request.Email);
                return Ok(new { message = "If your email is registered, you will receive a reset link." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            try
            {
                await _authService.ResetPasswordAsync(request);
                return Ok(new { message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}