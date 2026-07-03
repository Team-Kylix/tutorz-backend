using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class HallRepository : GenericRepository<Hall>, IHallRepository
    {
        public HallRepository(TutorzDbContext context) : base(context)
        {
        }
    }
}
