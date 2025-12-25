using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Student;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IStudentRepository : IGenericRepository<Student>
    {
        Task<List<ClassSearchDto>> SearchClassesAsync(string grade, string searchTerm);
        Task<string> RequestJoinClassAsync(Guid studentId, Guid classId);
    }
}
