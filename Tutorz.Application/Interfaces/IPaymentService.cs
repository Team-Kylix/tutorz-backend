using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Payment;

namespace Tutorz.Application.Interfaces
{
    public interface IPaymentService
    {
        /// <summary>
        /// Returns the last 12 months + next 3 months of payment status
        /// for a given student+class combination, relative to today.
        /// </summary>
        Task<ServiceResponse<IEnumerable<MonthPaymentStatusDto>>> GetPaymentStatusAsync(
            Guid classId, Guid studentId, Guid instituteId);

        /// <summary>
        /// Records a class fee payment. Rejects duplicates for the same month/year.
        /// </summary>
        Task<ServiceResponse<ClassPaymentDto>> RecordPaymentAsync(
            RecordPaymentRequest request, Guid instituteId);

        /// <summary>
        /// Gets the payment history for a specific class, ordered by most recent first.
        /// Identical filtering logic to attendance history.
        /// </summary>
        Task<ServiceResponse<FinancialHistoryResponseDto>> GetClassPaymentHistoryAsync(
            Guid instituteId, Guid? tutorId, Guid? classId, string? searchQuery = null, int page = 1, int pageSize = 10);

        /// <summary>
        /// Gets the payment history for a tutor's own classes, optionally filtered by
        /// instituteId (null = all, Guid.Empty = own-place/no-institute) and classId.
        /// </summary>
        Task<ServiceResponse<FinancialHistoryResponseDto>> GetTutorPaymentHistoryAsync(
            Guid tutorId, Guid? instituteId, bool noInstitute, Guid? classId, string? searchQuery = null, int page = 1, int pageSize = 10);
    }
}
