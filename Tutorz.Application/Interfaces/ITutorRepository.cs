using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Application.Interfaces
{
    public interface ITutorRepository : IGenericRepository<Tutor>
    {

        Task<TutorProfileDto> GetTutorProfileAsync(Guid userId);
        // Add specific tutor methods here later
    }
}
