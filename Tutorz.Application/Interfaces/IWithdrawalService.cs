using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Withdrawal;

namespace Tutorz.Application.Interfaces
{
    public interface IWithdrawalService
    {
        Task<ServiceResponse<decimal>> GetAvailableBalanceAsync(Guid tutorId, Guid instituteId);
        Task<ServiceResponse<IEnumerable<WithdrawalDto>>> GetTutorWithdrawalsAsync(Guid tutorId, Guid? instituteId);
        Task<ServiceResponse<IEnumerable<WithdrawalDto>>> GetInstituteWithdrawalsAsync(Guid instituteId, Guid? tutorId);
        Task<ServiceResponse<bool>> NotifyInstituteForWithdrawalAsync(Guid tutorId, WithdrawalRequestDto request);
        Task<ServiceResponse<WithdrawalDto>> ProcessWithdrawalAsync(Guid instituteId, WithdrawalProcessDto dto);
        Task<byte[]> GenerateWithdrawalPdfAsync(Guid withdrawalId);

        /// <summary>
        /// Returns one overview row per institute (for a tutor), filtered by optional instituteId/classId.
        /// Each row includes balance, last withdrawal ref/amount, and withdrawal period.
        /// </summary>
        Task<ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>> GetTutorWithdrawalOverviewAsync(
            Guid tutorId, Guid? instituteId);

        /// <summary>
        /// Returns one overview row per tutor (for an institute), filtered by optional tutorId/classId.
        /// </summary>
        Task<ServiceResponse<IEnumerable<WithdrawalOverviewRowDto>>> GetInstituteWithdrawalOverviewAsync(
            Guid instituteId, Guid? tutorId);

        Task<byte[]> GeneratePendingEarningsPdfAsync(Guid tutorId, Guid? instituteId);
        Task<byte[]> GenerateInstitutePendingEarningsPdfAsync(Guid instituteId, Guid? tutorId);

        Task<ServiceResponse<IEnumerable<MonthlyFeeRowDto>>> GetTutorMonthlyFeesAsync(Guid tutorId, Guid? instituteId);
        Task<ServiceResponse<IEnumerable<MonthlyFeeRowDto>>> GetInstituteMonthlyFeesAsync(Guid instituteId, Guid? tutorId);
        Task<byte[]> GenerateMonthlyFeesPdfAsync(Guid tutorId, Guid? instituteId, int year, int month);
        Task<byte[]> GenerateInstituteMonthlyFeesPdfAsync(Guid instituteId, Guid? tutorId, int year, int month);
    }
}
