using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Financials;
using Tutorz.Application.DTOs.Payment;
using Tutorz.Application.Interfaces;
using Tutorz.Api.Attributes;

namespace Tutorz.Api.Controllers
{
    /// <summary>
    /// Manages secure financial details (encrypted bank accounts + PayHere card tokens).
    ///
    /// Security notes:
    /// - All endpoints require authentication (JWT Bearer).
    /// - Bank account numbers are NEVER returned to the client — only masked versions.
    /// - CVV is never transmitted to this backend (handled entirely by PayHere on the frontend).
    /// - The actual AES decryption key never leaves the server process memory.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Any authenticated user (Tutor, Institute, Student)
    public class FinancialsController : ControllerBase
    {
        private readonly IFinancialsService _financialsService;

        public FinancialsController(IFinancialsService financialsService)
        {
            _financialsService = financialsService;
        }

        // ─────────────────────────────────────────────
        //  BANK DIRECTORY (public lookup for dropdowns)
        // ─────────────────────────────────────────────

        [HttpGet("banks")]
        [ApiPurpose("Get All Banks")]
        public async Task<IActionResult> GetBanks()
        {
            var result = await _financialsService.GetBanksAsync();
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("banks/{bankCode:int}/branches")]
        [ApiPurpose("Get Branches By Bank")]
        public async Task<IActionResult> GetBranches(int bankCode)
        {
            var result = await _financialsService.GetBranchesByBankAsync(bankCode);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ─────────────────────────────────────────────
        //  FINANCIAL SUMMARY (masked data for current user)
        // ─────────────────────────────────────────────

        [HttpGet("summary")]
        [ApiPurpose("Get Financial Summary")]
        public async Task<IActionResult> GetSummary()
        {
            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty) return Unauthorized("User identity not found in token.");

            var result = await _financialsService.GetFinancialSummaryAsync(ownerId, role);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ─────────────────────────────────────────────
        //  BANK DETAILS (Tutor + Institute only)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Save encrypted bank account details.
        /// The raw account number in the DTO is encrypted server-side and immediately discarded;
        /// only the ciphertext and masked version are persisted.
        /// </summary>
        [HttpPost("bank-details")]
        [ApiPurpose("Save Bank Details")]
        public async Task<IActionResult> SaveBankDetails([FromBody] SaveBankDetailsDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty) return Unauthorized("User identity not found in token.");

            if (role == "Student")
                return BadRequest(new { message = "Students cannot add bank accounts. Please add a card instead." });

            var result = await _financialsService.SaveBankDetailsAsync(ownerId, role, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("bank-details")]
        [ApiPurpose("Remove Bank Details")]
        public async Task<IActionResult> RemoveBankDetails()
        {
            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty) return Unauthorized("User identity not found in token.");

            var result = await _financialsService.RemoveBankDetailsAsync(ownerId, role);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ─────────────────────────────────────────────
        //  CARD TOKEN (all roles)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Saves the PayHere token + card metadata.
        /// The actual card number is NEVER sent to this endpoint — the frontend
        /// calls PayHere directly and receives a token. We store only that token + last4 + brand.
        /// </summary>
        [HttpPost("card-token")]
        [ApiPurpose("Save Card Token")]
        public async Task<IActionResult> SaveCardToken([FromBody] SaveCardTokenDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty) return Unauthorized("User identity not found in token.");

            var result = await _financialsService.SaveCardTokenAsync(ownerId, role, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("card-token")]
        [ApiPurpose("Remove Card Token")]
        public async Task<IActionResult> RemoveCardToken()
        {
            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty) return Unauthorized("User identity not found in token.");

            var result = await _financialsService.RemoveCardTokenAsync(ownerId, role);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // ─────────────────────────────────────────────
        //  ONLINE PAYMENTS (PAYHERE)
        // ─────────────────────────────────────────────

        // ─────────────────────────────────────────────
        //  PAYHERE PREAPPROVAL (Card Tokenization)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Returns the PayHere SDK preapproval parameters (hash, order_id, etc.).
        /// The frontend uses these to open window.payhere.startPayment({ preapprove: true }).
        /// </summary>
        [HttpPost("initiate-preapproval")]
        [ApiPurpose("Initiate PayHere Card Preapproval")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePreapproval()
        {
            var (studentId, role) = GetOwnerContext();
            if (studentId == Guid.Empty || role != "Student")
                return Unauthorized("Only students can initiate card preapproval.");

            var result = await _financialsService.InitiatePreapprovalAsync(studentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// PayHere calls this server-to-server after a successful preapproval.
        /// Verifies the hash and stores the customer_token on the student's record.
        /// Must be AllowAnonymous because PayHere does not send a JWT.
        /// </summary>
        [HttpPost("preapproval-notify")]
        [AllowAnonymous]
        public async Task<IActionResult> PreapprovalNotify([FromForm] PreapprovalNotifyDto notifyDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));
                Console.WriteLine($"[PayHere Webhook 400 ERROR] Validation Failed: {errors}");
                return BadRequest(errors);
            }

            var result = await _financialsService.ProcessPreapprovalNotifyAsync(notifyDto);
            // Always return 200 OK so PayHere doesn't retry indefinitely
            return Ok();
        }

        // ─────────────────────────────────────────────
        //  ONLINE PAYMENTS (PAYHERE CHARGING)
        // ─────────────────────────────────────────────

        [HttpGet("student-payment-status")]
        [ApiPurpose("Get Student Payment Status Month Strip")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> GetStudentPaymentStatus([FromQuery] Guid classId)
        {
            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty || role != "Student") 
                return Unauthorized("Only students can check fee statuses via this endpoint.");

            var result = await _financialsService.GetStudentPaymentStatusAsync(classId, ownerId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("initiate-payment")]
        [ApiPurpose("Initiate Online Payment")]
        [Authorize(Roles = "Student")]
        public async Task<IActionResult> InitiatePayment([FromBody] InitiatePaymentRequestDto request)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (ownerId, role) = GetOwnerContext();
            if (ownerId == Guid.Empty || role != "Student") 
                return Unauthorized("Only students can initiate fee payments.");

            var result = await _financialsService.InitiateOnlinePaymentAsync(ownerId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("payhere-notify")]
        [AllowAnonymous]
        public async Task<IActionResult> PayHereNotify([FromForm] PayHereNotifyDto notifyDto)
        {
            // Webhook payload comes as URL-encoded form data
            // We ignore failures because PayHere just expects a 200 OK generally
            var result = await _financialsService.ProcessPayHereWebhookAsync(notifyDto);
            
            // Regardless of logic internal failure, we return 200 OK so PayHere doesn't retry infinitely 
            // unless it's a critical server error. But to be safe, return Ok.
            return Ok();
        }

        // ─────────────────────────────────────────────
        //  Private Helpers
        // ─────────────────────────────────────────────

        /// <summary>
        /// Extracts the owner's primary Guid and role from JWT claims.
        /// Checks role-specific claims first (TutorId, InstituteId, StudentId) then falls back
        /// to NameIdentifier.
        /// </summary>
        private (Guid ownerId, string role) GetOwnerContext()
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Try role-specific ID claims first (most reliable)
            string? idStr = role switch
            {
                "Tutor"     => User.FindFirst("TutorId")?.Value,
                "Institute" => User.FindFirst("InstituteId")?.Value,
                "Student"   => User.FindFirst("StudentId")?.Value,
                _           => null
            };

            // Fall back to NameIdentifier
            if (string.IsNullOrEmpty(idStr))
                idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idStr) || !Guid.TryParse(idStr, out var id))
                return (Guid.Empty, role);

            return (id, role);
        }
    }
}
