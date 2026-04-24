using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tutorz.Application.Interfaces;
using Tutorz.Domain.Entities;
using Tutorz.Infrastructure.Data;

namespace Tutorz.Infrastructure.Repositories
{
    public class ClassPaymentRepository : GenericRepository<ClassPayment>, IClassPaymentRepository
    {
        public ClassPaymentRepository(TutorzDbContext context) : base(context)
        {
        }

        public async Task<decimal> GetTotalReceivedAsync(List<Guid> classIds, List<Guid> studentIds, int? year, int? month)
        {
            var query = _context.ClassPayments.AsNoTracking()
                .Where(p => classIds.Contains(p.ClassId) && studentIds.Contains(p.StudentId));

            if (year.HasValue) query = query.Where(p => p.Year == year.Value);
            if (month.HasValue) query = query.Where(p => p.Month == month.Value);

            return await query.SumAsync(p => p.AmountPaid);
        }
    }
}
