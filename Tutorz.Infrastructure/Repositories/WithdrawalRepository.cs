using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class WithdrawalRepository : GenericRepository<Withdrawal>, IWithdrawalRepository
    {
        public WithdrawalRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<Withdrawal?> GetByReferenceAsync(string referenceId)
        {
            return await _context.Withdrawals
                .Include(w => w.Institute)
                .Include(w => w.Tutor)
                .FirstOrDefaultAsync(w => w.ReferenceId == referenceId);
        }
    }
}
