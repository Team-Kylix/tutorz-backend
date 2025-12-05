using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly TutorzDbContext _context;

        public GenericRepository(TutorzDbContext context)
        {
            _context = context;
        }

        public async Task<T> GetAsync(Expression<Func<T, bool>> expression)
        {
            // Find the first item matching the expression
            return await _context.Set<T>().FirstOrDefaultAsync(expression);
        }

        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
        }

        public async Task<int> SaveChangesAsync()
        {
            // Save all changes made to the context
            return await _context.SaveChangesAsync();
        }
    }
}
