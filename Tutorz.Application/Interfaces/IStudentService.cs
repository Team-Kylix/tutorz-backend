using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Student;

namespace Tutorz.Application.Interfaces
{
    public interface IStudentService
    {
        Task<ServiceResponse<List<ClassSearchDto>>> SearchClassesAsync(string grade, string searchTerm);
        Task<ServiceResponse<string>> RequestJoinClassAsync(Guid studentId, Guid classId);
    }
}
