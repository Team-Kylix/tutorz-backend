using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class InstituteStudentRepository : IInstituteStudentRepository
    {
        private readonly TutorzDbContext _context;

        public InstituteStudentRepository(TutorzDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(InstituteStudent entity)
        {
            await _context.InstituteStudents.AddAsync(entity);
        }

        public async Task<IEnumerable<InstituteStudent>> GetAllAsync(Expression<Func<InstituteStudent, bool>> predicate = null)
        {
            if (predicate == null)
                return await _context.InstituteStudents.ToListAsync();
            return await _context.InstituteStudents.Where(predicate).ToListAsync();
        }

        public async Task<InstituteStudent> GetAsync(Expression<Func<InstituteStudent, bool>> predicate)
        {
            return await _context.InstituteStudents.FirstOrDefaultAsync(predicate);
        }

        public void Remove(InstituteStudent entity)
        {
            _context.InstituteStudents.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
