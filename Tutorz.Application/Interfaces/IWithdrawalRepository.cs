using System;
using System.Threading.Tasks;
using Tutorz.Domain.Entities;

namespace Tutorz.Application.Interfaces
{
    public interface IWithdrawalRepository : IGenericRepository<Withdrawal>
    {
        Task<Withdrawal?> GetByReferenceAsync(string referenceId);
    }
}
