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
    public class InstituteTutorRepository : IInstituteTutorRepository
    {
        private readonly TutorzDbContext _context;

        public InstituteTutorRepository(TutorzDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(InstituteTutor entity)
        {
            await _context.InstituteTutors.AddAsync(entity);
        }

        public async Task<IEnumerable<InstituteTutor>> GetAllAsync(Expression<Func<InstituteTutor, bool>> predicate = null)
        {
            if (predicate == null)
                return await _context.InstituteTutors.ToListAsync();
            return await _context.InstituteTutors.Where(predicate).ToListAsync();
        }

        public async Task<InstituteTutor> GetAsync(Expression<Func<InstituteTutor, bool>> predicate)
        {
            return await _context.InstituteTutors.FirstOrDefaultAsync(predicate);
        }

        public void Remove(InstituteTutor entity)
        {
            _context.InstituteTutors.Remove(entity);
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
