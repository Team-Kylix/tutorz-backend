using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Auth;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<ServiceResponse<bool>> CreateAdminAsync(Tutorz.Application.DTOs.System.CreateAdminDto request);
        Task SendRegistrationOtpAsync(string phoneNumber);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request);
        Task<bool> CheckEmailExistsAsync(string email);
        Task ForgotPasswordAsync(string identifier);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task<ServiceResponse<CheckUserResponse>> CheckUserStatusAsync(CheckUserRequest request);
        Task<AuthResponse> RegisterSiblingAsync(SiblingRegistrationRequest request);
        Task<AuthResponse> SwitchProfileAsync(Guid userId, Guid targetStudentId);
        Task SendOtpAsync(CheckUserRequest request);
        Task<VerifyUserResponse> VerifyOtpAsync(VerifyUserRequest request);
        
        // --- Credential Updates ---
        Task RequestEmailUpdateAsync(Guid userId, string newEmail);
        Task<ServiceResponse<bool>> VerifyEmailUpdateAsync(Guid userId, VerifyCredentialUpdateDto request);
        Task RequestMobileUpdateAsync(Guid userId, string newMobile);
        Task<ServiceResponse<bool>> VerifyMobileUpdateAsync(Guid userId, VerifyCredentialUpdateDto request);
        Task<ServiceResponse<bool>> ChangePasswordAsync(Guid userId, ChangePasswordDto request);
    }
}