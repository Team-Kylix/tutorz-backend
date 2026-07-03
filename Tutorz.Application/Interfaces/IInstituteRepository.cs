using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities; 

namespace Tutorz.Application.Interfaces
{
   
    public interface IInstituteRepository : IGenericRepository<Institute>
    {
        Task<Tutorz.Application.DTOs.Common.PaginatedResultDto<Tutorz.Application.DTOs.Institute.InstituteProfileDto>> GetAllInstitutesAsync(string? searchQuery, int page, int pageSize);
    }
}