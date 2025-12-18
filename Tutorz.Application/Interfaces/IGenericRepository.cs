using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Tutorz.Application.Interfaces
{
    public interface IGenericRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> expression = null, string includeProperties = "");

        Task<T> GetAsync(Expression<Func<T, bool>> expression, string includeProperties = "");

        Task AddAsync(T entity);

        Task<int> SaveChangesAsync();

        Task DeleteAsync(T entity);
    }
}