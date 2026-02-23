using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Interfaces
{
    public interface IInstituteService
    {
        Task<ServiceResponse<InstituteProfileDto>> GetProfileAsync(Guid instituteId);
        Task<ServiceResponse<InstituteProfileDto>> UpdateProfileAsync(Guid instituteId, UpdateInstituteProfileDto dto);

        // Assignments
        Task<ServiceResponse<bool>> AssignStudentAsync(Guid instituteId, AssignStudentDto dto);
        Task<ServiceResponse<bool>> AssignTutorAsync(Guid instituteId, AssignTutorDto dto);

        // Searching
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid instituteId, string query);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid instituteId, string query);

        // Get Assigned Users
        Task<ServiceResponse<IEnumerable<StudentProfileDto>>> GetAssignedStudentsAsync(Guid instituteId);
        Task<ServiceResponse<IEnumerable<TutorProfileDto>>> GetAssignedTutorsAsync(Guid instituteId);
    }
}