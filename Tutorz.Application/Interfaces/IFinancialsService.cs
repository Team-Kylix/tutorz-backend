using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Financials;
using Tutorz.Application.DTOs.Payment;

namespace Tutorz.Application.Interfaces
{
    /// <summary>
    /// Service for managing financial details (bank accounts + payment card tokens).
    /// All bank values are encrypted before persistence; only masked data is returned to clients.
    /// Card tokenization is handled via PayHere Preapproval API.
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

        // --- PayHere Preapproval (card tokenization) ---
        /// <summary>
        /// Generates the PayHere preapproval parameters (hash, order_id, etc.) for the frontend JS SDK.
        /// The frontend uses these to open the PayHere preapproval popup.
        /// </summary>
        Task<ServiceResponse<object>> InitiatePreapprovalAsync(Guid ownerId, string role);

        /// <summary>
        /// Handles the PayHere preapproval notify_url callback.
        /// Verifies the md5sig, extracts the customer_token and stores it on the student record.
        /// </summary>
        Task<ServiceResponse<bool>> ProcessPreapprovalNotifyAsync(PreapprovalNotifyDto notify);

        // --- Online Payments (PayHere) ---
        Task<ServiceResponse<IEnumerable<MonthPaymentStatusDto>>> GetStudentPaymentStatusAsync(Guid classId, Guid studentId);
        Task<ServiceResponse<object>> InitiateOnlinePaymentAsync(Guid studentId, InitiatePaymentRequestDto request);
        Task<ServiceResponse<object>> InitiateBillPaymentAsync(Guid ownerId, string role, InitiateBillPaymentRequestDto request);
        Task<ServiceResponse<bool>> ProcessPayHereWebhookAsync(PayHereNotifyDto notifyDto);
    }
}
