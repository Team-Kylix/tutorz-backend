using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IInstituteStudentRepository
    {
        Task<InstituteStudent> GetAsync(Expression<Func<InstituteStudent, bool>> predicate);
        Task<IEnumerable<InstituteStudent>> GetAllAsync(Expression<Func<InstituteStudent, bool>> predicate = null);
        Task AddAsync(InstituteStudent entity);
        void Remove(InstituteStudent entity);
        Task SaveChangesAsync();
    }
}
