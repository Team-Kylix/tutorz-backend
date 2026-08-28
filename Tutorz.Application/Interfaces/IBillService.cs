using System;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;

namespace Tutorz.Application.Interfaces
{
    public interface IBillService
    {
        Task<ServiceResponse<BillPagedResult>> GetMyBillsAsync(Guid userId, int page, int pageSize);
        Task<ServiceResponse<byte[]>> GenerateBillPdfAsync(Guid billId, Guid requestingUserId);
        
        Task<ServiceResponse<BillPagedResult>> GetAllBillsAsync(string? search, int page, int pageSize);
        Task<ServiceResponse<BillDetailDto>> GetBillByIdAsync(Guid billId, Guid requestingUserId, string requestingRole);
        Task<ServiceResponse<bool>> MarkBillAsPaidAsync(Guid billId);
    }
}
