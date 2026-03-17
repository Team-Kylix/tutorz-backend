using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class InstituteJoinRequestRepository : GenericRepository<InstituteJoinRequest>, IInstituteJoinRequestRepository
    {
        public InstituteJoinRequestRepository(TutorzDbContext context) : base(context)
        {
        }
    }
}
