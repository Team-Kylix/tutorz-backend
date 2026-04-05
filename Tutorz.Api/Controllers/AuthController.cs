using Microsoft.AspNetCore.Mvc;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Application.Interfaces;
using System;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using Tutorz.Api.Attributes;

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
        [ApiPurpose("Register Account")]
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
        [ApiPurpose("User Login")]
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
                await _authService.SendOtpAsync(request);
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
                await _authService.ForgotPasswordAsync(request.Identifier);
                return Ok(new { message = "If your account exists, you will receive a reset code." });
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

        // --- CREDENTIAL UPDATES (Authenticated) ---
        [HttpPost("request-email-update")]
        [Authorize]
        [ApiPurpose("Request Email Update")]
        public async Task<IActionResult> RequestEmailUpdate([FromBody] RequestCredentialUpdateDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                await _authService.RequestEmailUpdateAsync(Guid.Parse(userIdClaim), request.NewIdentifier);
                return Ok(new { message = "OTP sent to new email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-email-update")]
        [Authorize]
        [ApiPurpose("Verify Email Update")]
        public async Task<IActionResult> VerifyEmailUpdate([FromBody] VerifyCredentialUpdateDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                var result = await _authService.VerifyEmailUpdateAsync(Guid.Parse(userIdClaim), request);
                if (!result.Success) return BadRequest(new { message = result.Message });
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("request-mobile-update")]
        [Authorize]
        [ApiPurpose("Request Mobile Update")]
        public async Task<IActionResult> RequestMobileUpdate([FromBody] RequestCredentialUpdateDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                await _authService.RequestMobileUpdateAsync(Guid.Parse(userIdClaim), request.NewIdentifier);
                return Ok(new { message = "OTP sent to new mobile number." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("verify-mobile-update")]
        [Authorize]
        [ApiPurpose("Verify Mobile Update")]
        public async Task<IActionResult> VerifyMobileUpdate([FromBody] VerifyCredentialUpdateDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                var result = await _authService.VerifyMobileUpdateAsync(Guid.Parse(userIdClaim), request);
                if (!result.Success) return BadRequest(new { message = result.Message });
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        [ApiPurpose("Change Password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
                if (string.IsNullOrEmpty(userIdClaim)) return Unauthorized();

                var result = await _authService.ChangePasswordAsync(Guid.Parse(userIdClaim), request);
                if (!result.Success) return BadRequest(new { message = result.Message });
                return Ok(new { message = result.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class ProfilePictureUploadRequest
        {
            public Guid EntityId { get; set; }
            public string RegistrationNumber { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public IFormFile? File { get; set; }
        }

        // --- PROFILE PICTURE UPLOAD ---
        [HttpPost("profile-picture")]
        [ApiPurpose("Upload Profile Picture")]
        public async Task<IActionResult> UploadProfilePicture(
            [FromForm] ProfilePictureUploadRequest request,
            [FromServices] IProfilePictureService profilePictureService)
        {
            try
            {
                if (request.File == null)
                    return BadRequest(new { message = "No file was uploaded." });

                var urls = await profilePictureService.UploadProfilePictureAsync(request.EntityId, request.RegistrationNumber, request.Role, request.File);
                return Ok(new { smallUrl = urls.smallUrl, largeUrl = urls.largeUrl, message = "Profile picture uploaded successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}