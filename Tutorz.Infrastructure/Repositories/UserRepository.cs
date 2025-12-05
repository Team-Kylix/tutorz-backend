using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    // This class inherits all the generic methods (GetAsync, AddAsync, etc.)
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        // We pass the DbContext to the base GenericRepository
        public UserRepository(TutorzDbContext context) : base(context)
        {
        }

        // You can add user-specific data logic here
    }

    // Create these implementation classes as well:
    // public class TutorRepository : GenericRepository<Tutor>, ITutorRepository { /* ...constructor... */ }
    // public class StudentRepository : GenericRepository<Student>, IStudentRepository { /* ...constructor... */ }
}
