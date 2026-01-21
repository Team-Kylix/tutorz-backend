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
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> SocialLoginAsync(SocialLoginRequest request);
        Task<bool> CheckEmailExistsAsync(string email);
        Task ForgotPasswordAsync(string email);
        Task ResetPasswordAsync(ResetPasswordRequest request);
        Task<ServiceResponse<CheckUserResponse>> CheckUserStatusAsync(CheckUserRequest request);
        Task<AuthResponse> RegisterSiblingAsync(SiblingRegistrationRequest request);
        Task<AuthResponse> SwitchProfileAsync(Guid userId, Guid targetStudentId);
        Task SendOtpAsync(string identifier);
        Task<VerifyUserResponse> VerifyOtpAsync(VerifyUserRequest request);
    }
}