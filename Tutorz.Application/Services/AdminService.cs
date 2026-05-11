using System;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Admin;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IGenericRepository<Admin> _adminRepository;
        private readonly IUserRepository _userRepository;
        private readonly IProfilePictureService _profilePictureService;
        private readonly IRoleRepository _roleRepository;

        public AdminService(
            IGenericRepository<Admin> adminRepository,
            IUserRepository userRepository,
            IProfilePictureService profilePictureService,
            IRoleRepository roleRepository)
        {
            _adminRepository = adminRepository;
            _userRepository = userRepository;
            _profilePictureService = profilePictureService;
            _roleRepository = roleRepository;
        }

        public async Task<ServiceResponse<AdminProfileDto>> GetAdminProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetAsync(u => u.UserId == userId);
            if (user == null)
                return new ServiceResponse<AdminProfileDto> { Success = false, Message = "User not found." };

            var admin = await _adminRepository.GetAsync(a => a.UserId == userId);
            if (admin == null)
                return new ServiceResponse<AdminProfileDto> { Success = false, Message = "Admin profile not found." };

            var role = await _roleRepository.GetAsync(r => r.RoleId == user.RoleId);

            var dto = new AdminProfileDto
            {
                AdminId = admin.AdminId,
                RegistrationNumber = admin.RegistrationNumber,
                FirstName = admin.FirstName,
                LastName = admin.LastName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Address = admin.Address,
                ProfileImageUrlSmall = admin.ProfileImageUrlSmall,
                ProfileImageUrlLarge = admin.ProfileImageUrlLarge,
                Role = role?.Name ?? "Admin",
                CreatedDate = admin.CreatedDate
            };

            return new ServiceResponse<AdminProfileDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<AdminProfileDto>> UpdateAdminProfileAsync(Guid userId, UpdateAdminProfileDto request)
        {
            var admin = await _adminRepository.GetAsync(a => a.UserId == userId);
            if (admin == null)
                return new ServiceResponse<AdminProfileDto> { Success = false, Message = "Admin profile not found." };

            admin.FirstName = request.FirstName;
            admin.LastName = request.LastName;
            admin.Address = request.Address;
            admin.UpdatedDate = DateTime.UtcNow;

            if (request.ProfilePicture != null)
            {
                var (smallUrl, largeUrl) = await _profilePictureService.UploadProfilePictureAsync(
                    admin.AdminId,
                    admin.RegistrationNumber,
                    "Admin",
                    request.ProfilePicture
                );

                admin.ProfileImageUrlSmall = smallUrl;
                admin.ProfileImageUrlLarge = largeUrl;
            }

            await _adminRepository.SaveChangesAsync();

            return await GetAdminProfileAsync(userId);
        }
    }
}
