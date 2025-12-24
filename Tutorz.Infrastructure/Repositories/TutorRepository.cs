using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore; // <--- REQUIRED for FirstOrDefaultAsync
using Tutorz.Application.DTOs.Tutor;

namespace Tutorz.Infrastructure.Repositories
{
    public class TutorRepository : GenericRepository<Tutor>, ITutorRepository
    {
        public TutorRepository(TutorzDbContext context) : base(context)
        {
        }
        public async Task<TutorProfileDto> GetTutorProfileAsync(Guid userId)
        {
            // We cast the base Context to your specific TutorzDbContext to access the "Users" table
            var db = _context as TutorzDbContext;

            return await (from t in db.Tutors
                          
                          join u in db.Users on t.UserId equals u.UserId
                          where t.UserId == userId
                          select new TutorProfileDto
                          {
                              FirstName = t.FirstName,
                              LastName = t.LastName,
                              Bio = t.Bio,
                              BankAccountNumber = t.BankAccountNumber,
                              BankName = t.BankName,
                              RegistrationNumber = t.RegistrationNumber,
                              Email = u.Email,
                              PhoneNumber = u.PhoneNumber
                          }).FirstOrDefaultAsync();
        }
    }
}
