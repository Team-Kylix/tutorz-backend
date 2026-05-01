using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tutorz.Application.DTOs;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;

namespace Tutorz.Application.Interfaces
{
    public interface IBillService
    {
        // Admin Endpoints
        Task<ServiceResponse<BillPagedResult>> GetAllBillsAsync(string? search, int page, int pageSize);
        Task<ServiceResponse<bool>> MarkBillAsPaidAsync(Guid billId);
        Task<ServiceResponse<bool>> RolloverOverdueBillsAsync(int targetMonth, int targetYear);
        Task<ServiceResponse<BillingConfigDto>> GetBillingConfigAsync();
        Task<ServiceResponse<bool>> UpdateBillingConfigAsync(BillingConfigDto config);

        // User Endpoints
        Task<ServiceResponse<BillPagedResult>> GetMyBillsAsync(Guid userId, int page, int pageSize);
        
        // Shared Endpoints
        Task<ServiceResponse<BillDetailDto>> GetBillByIdAsync(Guid billId, Guid requestingUserId, string requestingRole);
        Task<byte[]?> GenerateBillPdfAsync(Guid billId, Guid requestingUserId, string requestingRole);
        Task<ServiceResponse<int>> FixOldBillReferencesAsync();


        // Incremental real-time billing updates
        Task IncrementPlatformCommissionAsync(Guid instituteId, Guid tutorId, decimal instituteCommission, decimal tutorCommission, int month, int year);
        Task IncrementSmsUsageAsync(Guid userId, int smsCount, decimal smsCost, DateTime date);
        Task IncrementApiUsageAsync(Guid userId, int apiCallCount, DateTime date);
    }
}
