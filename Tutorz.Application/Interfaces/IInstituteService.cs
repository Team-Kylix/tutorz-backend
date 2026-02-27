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

        // Join Requests
        Task<ServiceResponse<bool>> SendTutorRequestAsync(Guid instituteId, AssignTutorDto dto);
        Task<ServiceResponse<IEnumerable<JoinRequestDto>>> GetIncomingRequestsAsync(Guid instituteId);
        Task<ServiceResponse<bool>> ProcessJoinRequestAsync(Guid instituteId, Guid requestId, string action);

        // Searching
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchStudentsAsync(Guid instituteId, string query);
        Task<ServiceResponse<IEnumerable<SearchUserResultDto>>> SearchTutorsAsync(Guid instituteId, string query);

        // Get Assigned Users (Paginated & Searched)
        Task<ServiceResponse<PaginatedResultDto<StudentProfileDto>>> GetAssignedStudentsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10);
        Task<ServiceResponse<PaginatedResultDto<TutorProfileDto>>> GetAssignedTutorsAsync(Guid instituteId, string searchQuery = "", int page = 1, int pageSize = 10);
    }
}