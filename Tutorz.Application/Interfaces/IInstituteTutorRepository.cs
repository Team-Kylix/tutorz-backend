using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IInstituteTutorRepository
    {
        Task<InstituteTutor> GetAsync(Expression<Func<InstituteTutor, bool>> predicate);
        Task<IEnumerable<InstituteTutor>> GetAllAsync(Expression<Func<InstituteTutor, bool>> predicate = null);
        Task AddAsync(InstituteTutor entity);
        void Remove(InstituteTutor entity);
        Task SaveChangesAsync();
    }
}
