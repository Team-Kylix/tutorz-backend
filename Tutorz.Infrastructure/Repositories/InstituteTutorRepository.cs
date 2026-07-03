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

        public async Task<IEnumerable<InstituteTutor>> GetAllAsync(Expression<Func<InstituteTutor, bool>> predicate = null, string includeProperties = "")
        {
            IQueryable<InstituteTutor> query = _context.InstituteTutors;

            if (predicate != null)
                query = query.Where(predicate);

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return await query.ToListAsync();
        }

        public async Task<InstituteTutor> GetAsync(Expression<Func<InstituteTutor, bool>> predicate, string includeProperties = "")
        {
            IQueryable<InstituteTutor> query = _context.InstituteTutors;
            
            query = query.Where(predicate);

            foreach (var includeProperty in includeProperties.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProperty);
            }

            return await query.FirstOrDefaultAsync();
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
