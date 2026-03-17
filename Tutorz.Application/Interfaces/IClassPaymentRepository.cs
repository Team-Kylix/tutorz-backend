using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IClassPaymentRepository : IGenericRepository<ClassPayment>
    {
        Task<decimal> GetTotalReceivedAsync(List<Guid> classIds, List<Guid> studentIds, int? year, int? month);
    }
}
