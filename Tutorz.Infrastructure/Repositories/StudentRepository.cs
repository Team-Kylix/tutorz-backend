using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;
using Tutorz.Application.Interfaces;

namespace Tutorz.Infrastructure.Repositories
{
    public class StudentRepository : GenericRepository<Student>, IStudentRepository
    {
        //Add the constructor
        public StudentRepository(TutorzDbContext context) : base(context)
        {
        }
    }
}
