using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Application.Interfaces;

namespace Tutorz.Application.Services
{
    public class InstituteService : IInstituteService
    {

        private readonly IInstituteRepository _instituteRepository;

        public InstituteService(IInstituteRepository instituteRepository)
        {
            _instituteRepository = instituteRepository;
        }

        public async Task<ServiceResponse<InstituteProfileDto>> GetProfileAsync(Guid instituteId)
        {
            
            var institute = await _instituteRepository.GetAsync(
                expression: i => i.InstituteId == instituteId || i.UserId == instituteId,
                includeProperties: "User"
            );

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

            var dto = new InstituteProfileDto
            {
                InstituteId = institute.InstituteId,
                RegistrationNumber = institute.RegistrationNumber,
                InstituteName = institute.InstituteName,
                Address = institute.Address,
                ContactNumber = institute.ContactNumber,
                Website = institute.Website,
                Email = (institute.User != null) ? institute.User.Email : ""
            };

            return new ServiceResponse<InstituteProfileDto> { Success = true, Data = dto };
        }

        public async Task<ServiceResponse<InstituteProfileDto>> UpdateProfileAsync(Guid id, UpdateInstituteProfileDto dto)
        {
            
            var institute = await _instituteRepository.GetAsync(i => i.InstituteId == id || i.UserId == id);

            if (institute == null)
                return new ServiceResponse<InstituteProfileDto> { Success = false, Message = "Institute not found." };

       
            institute.InstituteName = dto.InstituteName;
            institute.Address = dto.Address;
            institute.ContactNumber = dto.ContactNumber;
            institute.Website = dto.Website;
            institute.UpdatedDate = DateTime.UtcNow;

            await _instituteRepository.SaveChangesAsync();

            
            return await GetProfileAsync(institute.InstituteId);
        }
    }
}