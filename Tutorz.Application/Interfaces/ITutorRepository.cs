using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Interfaces
{
    public interface ITutorRepository : IGenericRepository<Tutor>
    {
        Task<TutorProfileDto> GetTutorProfileAsync(Guid userId);
        Task<List<StudentRequestDto>> GetPendingRequestsAsync(Guid tutorUserId);
        Task<List<Enrollment>> GetEnrollmentsByIdsAsync(List<Guid> enrollmentIds);
        Task UpdateEnrollmentsAsync(List<Enrollment> enrollments);
        Task<StudentFullProfileDto> GetStudentProfileForTutorAsync(Guid studentId);
    }
}