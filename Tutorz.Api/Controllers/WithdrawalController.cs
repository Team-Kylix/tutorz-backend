using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

using Tutorz.Application.DTOs.Withdrawal;
using Tutorz.Application.Interfaces;
using Tutorz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class WithdrawalController : ControllerBase
    {
        private readonly IWithdrawalService _withdrawalService;
        private readonly TutorzDbContext _context;

        public WithdrawalController(IWithdrawalService withdrawalService, TutorzDbContext context)
        {
            _withdrawalService = withdrawalService;
            _context = context;
        }

        /// <summary>
        /// Returns the UserId from JWT (sub claim).
        /// </summary>
        private Guid GetUserId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(val, out var id) ? id : Guid.Empty;
        }

        private string GetRole() =>
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        // ─── Resolve TutorId from UserId ────────────────────────────
        private async Task<Guid> GetTutorIdAsync(Guid userId)
        {
            var tutor = await _context.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);
            return tutor?.TutorId ?? Guid.Empty;
        }

        // ─── Resolve InstituteId from UserId ────────────────────────
        private async Task<Guid> GetInstituteIdAsync(Guid userId)
        {
            var institute = await _context.Institutes.FirstOrDefaultAsync(i => i.UserId == userId);
            return institute?.InstituteId ?? Guid.Empty;
        }

        // GET /api/withdrawal/tutor?instituteId=
        [HttpGet("tutor")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> GetTutorWithdrawals([FromQuery] Guid? instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.GetTutorWithdrawalsAsync(tutorId, instituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/institute?tutorId=
        [HttpGet("institute")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> GetInstituteWithdrawals([FromQuery] Guid? tutorId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.GetInstituteWithdrawalsAsync(instituteId, tutorId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/balance
        // Tutor: pass ?instituteId=  (tutorId resolved from JWT)
        // Institute: pass ?tutorId=  (instituteId resolved from JWT)
        [HttpGet("balance")]

        public async Task<IActionResult> GetAvailableBalance([FromQuery] Guid? instituteId, [FromQuery] Guid? tutorId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var role = GetRole();
            Guid resolvedTutorId;
            Guid resolvedInstituteId;

            if (role == "Tutor")
            {
                resolvedTutorId = await GetTutorIdAsync(userId);
                if (resolvedTutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

                if (!instituteId.HasValue || instituteId.Value == Guid.Empty)
                    return BadRequest(new { message = "instituteId is required for Tutor balance check." });

                resolvedInstituteId = instituteId.Value;
            }
            else if (role == "Institute")
            {
                resolvedInstituteId = await GetInstituteIdAsync(userId);
                if (resolvedInstituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

                if (!tutorId.HasValue || tutorId.Value == Guid.Empty)
                    return BadRequest(new { message = "tutorId is required for Institute balance check." });

                resolvedTutorId = tutorId.Value;
            }
            else
            {
                return Forbid();
            }

            var result = await _withdrawalService.GetAvailableBalanceAsync(resolvedTutorId, resolvedInstituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // POST /api/withdrawal/request-notification
        [HttpPost("request-notification")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> RequestWithdrawalNotification([FromBody] WithdrawalRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.NotifyInstituteForWithdrawalAsync(tutorId, dto);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = "Withdrawal request notification sent successfully." });
        }

        // POST /api/withdrawal/process
        [HttpPost("process")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> ProcessWithdrawal([FromBody] WithdrawalProcessDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.ProcessWithdrawalAsync(instituteId, dto);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = "Withdrawal processed successfully.", data = result.Data });
        }

        // GET /api/withdrawal/{id}/pdf
        [HttpGet("{id}/pdf")]

        public async Task<IActionResult> DownloadWithdrawalPdf(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var pdfBytes = await _withdrawalService.GenerateWithdrawalPdfAsync(id);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No receipt data found for the given withdrawal." });

            return File(pdfBytes, "application/pdf", $"Withdrawal_Receipt_{id}.pdf");
        }

        // GET /api/withdrawal/overview?instituteId=
        [HttpGet("overview")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> GetTutorOverview([FromQuery] Guid? instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.GetTutorWithdrawalOverviewAsync(tutorId, instituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/overview-institute?tutorId=
        [HttpGet("overview-institute")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> GetInstituteOverview([FromQuery] Guid? tutorId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.GetInstituteWithdrawalOverviewAsync(instituteId, tutorId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }
        // GET /api/withdrawal/overview-pdf?instituteId=
        [HttpGet("overview-pdf")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> DownloadTutorOverviewPdf([FromQuery] Guid? instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var pdfBytes = await _withdrawalService.GeneratePendingEarningsPdfAsync(tutorId, instituteId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No pending earnings found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Pending_Earnings_Report.pdf");
        }

        // GET /api/withdrawal/overview-institute-pdf?tutorId=
        [HttpGet("overview-institute-pdf")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> DownloadInstituteOverviewPdf([FromQuery] Guid? tutorId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var pdfBytes = await _withdrawalService.GenerateInstitutePendingEarningsPdfAsync(instituteId, tutorId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No pending payouts found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Pending_Payouts_Report.pdf");
        }
        // GET /api/withdrawal/tutor/fees
        [HttpGet("tutor/fees")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> GetTutorMonthlyFees([FromQuery] Guid? instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.GetTutorMonthlyFeesAsync(tutorId, instituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/institute/fees
        [HttpGet("institute/fees")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> GetInstituteMonthlyFees([FromQuery] Guid? tutorId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.GetInstituteMonthlyFeesAsync(instituteId, tutorId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/tutor/fees/pdf
        [HttpGet("tutor/fees/pdf")]
        [Authorize(Roles = "Tutor")]

        public async Task<IActionResult> DownloadTutorMonthlyFeesPdf([FromQuery] Guid? instituteId, [FromQuery] int year, [FromQuery] int month)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var pdfBytes = await _withdrawalService.GenerateMonthlyFeesPdfAsync(tutorId, instituteId, year, month);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No fees found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Fees_Report_{year}_{month}.pdf");
        }

        // GET /api/withdrawal/institute/fees/pdf
        [HttpGet("institute/fees/pdf")]
        [Authorize(Roles = "Institute")]

        public async Task<IActionResult> DownloadInstituteMonthlyFeesPdf([FromQuery] Guid? tutorId, [FromQuery] int year, [FromQuery] int month)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var pdfBytes = await _withdrawalService.GenerateInstituteMonthlyFeesPdfAsync(instituteId, tutorId, year, month);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No fees found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Fees_Report_{year}_{month}.pdf");
        }

        // ============================================================
        // EARNINGS SUMMARIES & WALLETS
        // ============================================================

        // POST /api/withdrawal/calculate
        [HttpPost("calculate")]
        [Authorize(Roles = "Tutor")]
        public async Task<IActionResult> CalculateEarnings([FromBody] CalculateEarningsDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.CalculateEarningsAsync(dto.Month, dto.Year, tutorId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = "Earnings calculated successfully.", data = result.Data });
        }

        // POST /api/withdrawal/calculate-institute
        [HttpPost("calculate-institute")]
        [Authorize(Roles = "Institute")]
        public async Task<IActionResult> CalculateInstituteEarnings([FromBody] CalculateInstituteEarningsDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.CalculateInstituteEarningsAsync(dto.Month, dto.Year, instituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = "Institute earnings calculated successfully.", data = result.Data });
        }

        // GET /api/withdrawal/earnings?tutorId=&instituteId=
        [HttpGet("earnings")]
        public async Task<IActionResult> GetEarningsSummaries([FromQuery] Guid? tutorId, [FromQuery] Guid? instituteId)
        {
            var result = await _withdrawalService.GetEarningsSummariesAsync(tutorId, instituteId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/wallet-balances
        [HttpGet("wallet-balances")]
        public async Task<IActionResult> GetWalletBalances()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _withdrawalService.GetWalletBalancesAsync(userId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(new { data = result.Data });
        }

        // POST /api/withdrawal/withdraw
        [HttpPost("withdraw")]
        public async Task<IActionResult> WithdrawFromWallet([FromBody] WithdrawDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = await _withdrawalService.WithdrawFromWalletAsync(dto.WalletId, dto.Amount, dto.Type, dto.Description);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = "Withdrawal processed successfully." });
        }
    }
}
