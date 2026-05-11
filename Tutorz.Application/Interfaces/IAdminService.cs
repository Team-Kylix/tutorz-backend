using System;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Admin;
using Tutorz.Application.DTOs.Common;

namespace Tutorz.Application.Interfaces
{
    public interface IAdminService
    {
        Task<ServiceResponse<AdminProfileDto>> GetAdminProfileAsync(Guid userId);
        Task<ServiceResponse<AdminProfileDto>> UpdateAdminProfileAsync(Guid userId, UpdateAdminProfileDto request);
    }
}
