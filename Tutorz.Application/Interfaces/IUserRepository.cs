using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        // You can add specific methods here later, e.g.:
        // Task<User> GetUserWithDetailsAsync(Guid id);
    }

    // You should create these too:
    // public interface ITutorRepository : IGenericRepository<Tutor> { }
    // public interface IStudentRepository : IGenericRepository<Student> { }
    // public interface IInstituteRepository : IGenericRepository<Institute> { }
}
