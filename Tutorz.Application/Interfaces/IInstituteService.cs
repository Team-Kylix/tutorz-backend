using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;

namespace Tutorz.Application.Interfaces
{
    public interface IInstituteService
    {
        Task<ServiceResponse<InstituteProfileDto>> GetProfileAsync(Guid instituteId);
        Task<ServiceResponse<InstituteProfileDto>> UpdateProfileAsync(Guid instituteId, UpdateInstituteProfileDto dto);
    }
}