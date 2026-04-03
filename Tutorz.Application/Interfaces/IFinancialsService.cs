using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Financials;

namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// Service for managing financial details (bank accounts + payment card tokens).
    /// All bank values are encrypted before persistence; only masked data is returned to clients.
    /// </summary>
    public interface IFinancialsService
    {
        // --- Bank Details (Tutors + Institutes only) ---
        Task<ServiceResponse<FinancialSummaryDto>> SaveBankDetailsAsync(Guid ownerId, string role, SaveBankDetailsDto dto);
        Task<ServiceResponse<bool>> RemoveBankDetailsAsync(Guid ownerId, string role);

        // --- Card / PayHere Token (all roles) ---
        Task<ServiceResponse<FinancialSummaryDto>> SaveCardTokenAsync(Guid ownerId, string role, SaveCardTokenDto dto);
        Task<ServiceResponse<bool>> RemoveCardTokenAsync(Guid ownerId, string role);

        // --- Read (returns masked data only) ---
        Task<ServiceResponse<FinancialSummaryDto>> GetFinancialSummaryAsync(Guid ownerId, string role);

        // --- Bank Directory (for dropdowns) ---
        Task<ServiceResponse<IEnumerable<BankDto>>> GetBanksAsync();
        Task<ServiceResponse<IEnumerable<BranchDto>>> GetBranchesByBankAsync(int bankCode);
    }
}
