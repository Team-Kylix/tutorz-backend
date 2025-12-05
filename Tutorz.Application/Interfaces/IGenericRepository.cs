using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Auth;

namespace Tutorz.Application.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        // Get a single entity by an expression (e.g., find user by email)
        Task<T> GetAsync(Expression<Func<T, bool>> expression);

        // Add a new entity
        Task AddAsync(T entity);

        // We need a way to save all changes
        Task<int> SaveChangesAsync();
    }
}
