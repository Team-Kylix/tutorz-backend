using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class InstituteRepository : GenericRepository<Institute>, IInstituteRepository
    {
        public InstituteRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<Tutorz.Application.DTOs.Common.PaginatedResultDto<Tutorz.Application.DTOs.Institute.InstituteProfileDto>> GetAllInstitutesAsync(string? searchQuery, int page, int pageSize)
        {
            var query = _context.Institutes
                .Include(i => i.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string term = searchQuery.ToLower();
                query = query.Where(i =>
                    i.InstituteName.ToLower().Contains(term) ||
                    i.RegistrationNumber.ToLower().Contains(term) ||
                    (i.ContactNumber != null && i.ContactNumber.Contains(term)) ||
                    (i.User != null && i.User.Email != null && i.User.Email.ToLower().Contains(term))
                );
            }

            var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.CountAsync(query);

            var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(query
                .OrderByDescending(i => i.User != null ? i.User.CreatedDate : System.DateTime.MinValue)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new Tutorz.Application.DTOs.Institute.InstituteProfileDto
                {
                    InstituteId = i.InstituteId,
                    RegistrationNumber = i.RegistrationNumber,
                    InstituteName = i.InstituteName,
                    Address = i.Address,
                    ContactNumber = i.ContactNumber,
                    Website = i.Website,
                    Email = i.User != null ? i.User.Email : "",
                    CityId = i.User != null ? i.User.CityId : null,
                    IsSmsEnabled = i.IsSmsEnabled,
                    CommissionPercentage = i.CommissionPercentage,
                    ProfileImageUrlSmall = i.ProfileImageUrlSmall,
                    ProfileImageUrlLarge = i.ProfileImageUrlLarge
                }));

            return new Tutorz.Application.DTOs.Common.PaginatedResultDto<Tutorz.Application.DTOs.Institute.InstituteProfileDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }
    }
}
