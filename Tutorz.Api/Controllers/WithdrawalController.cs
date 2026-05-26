using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Api.Attributes;
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

        // GET /api/withdrawal/tutor?instituteId=&classId=
        [HttpGet("tutor")]
        [Authorize(Roles = "Tutor")]
        [ApiPurpose("Get Tutor Withdrawals")]
        public async Task<IActionResult> GetTutorWithdrawals([FromQuery] Guid? instituteId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.GetTutorWithdrawalsAsync(tutorId, instituteId, classId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/institute?tutorId=&classId=
        [HttpGet("institute")]
        [Authorize(Roles = "Institute")]
        [ApiPurpose("Get Institute Withdrawals")]
        public async Task<IActionResult> GetInstituteWithdrawals([FromQuery] Guid? tutorId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.GetInstituteWithdrawalsAsync(instituteId, tutorId, classId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/balance
        // Tutor: pass ?instituteId=  (tutorId resolved from JWT)
        // Institute: pass ?tutorId=  (instituteId resolved from JWT)
        [HttpGet("balance")]
        [ApiPurpose("Get Available Balance for Withdrawal")]
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
        [ApiPurpose("Request Withdrawal Notification")]
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
        [ApiPurpose("Process Tutor Withdrawal")]
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
        [ApiPurpose("Download Withdrawal PDF")]
        public async Task<IActionResult> DownloadWithdrawalPdf(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var pdfBytes = await _withdrawalService.GenerateWithdrawalPdfAsync(id);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No receipt data found for the given withdrawal." });

            return File(pdfBytes, "application/pdf", $"Withdrawal_Receipt_{id}.pdf");
        }

        // GET /api/withdrawal/overview?instituteId=&classId=
        [HttpGet("overview")]
        [Authorize(Roles = "Tutor")]
        [ApiPurpose("Get Tutor Withdrawal Overview")]
        public async Task<IActionResult> GetTutorOverview([FromQuery] Guid? instituteId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var result = await _withdrawalService.GetTutorWithdrawalOverviewAsync(tutorId, instituteId, classId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }

        // GET /api/withdrawal/overview-institute?tutorId=&classId=
        [HttpGet("overview-institute")]
        [Authorize(Roles = "Institute")]
        [ApiPurpose("Get Institute Withdrawal Overview")]
        public async Task<IActionResult> GetInstituteOverview([FromQuery] Guid? tutorId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var result = await _withdrawalService.GetInstituteWithdrawalOverviewAsync(instituteId, tutorId, classId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { data = result.Data });
        }
        // GET /api/withdrawal/overview-pdf?instituteId=&classId=
        [HttpGet("overview-pdf")]
        [Authorize(Roles = "Tutor")]
        [ApiPurpose("Download Pending Earnings PDF")]
        public async Task<IActionResult> DownloadTutorOverviewPdf([FromQuery] Guid? instituteId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutorId = await GetTutorIdAsync(userId);
            if (tutorId == Guid.Empty) return NotFound(new { message = "Tutor profile not found." });

            var pdfBytes = await _withdrawalService.GeneratePendingEarningsPdfAsync(tutorId, instituteId, classId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No pending earnings found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Pending_Earnings_Report.pdf");
        }

        // GET /api/withdrawal/overview-institute-pdf?tutorId=&classId=
        [HttpGet("overview-institute-pdf")]
        [Authorize(Roles = "Institute")]
        [ApiPurpose("Download Pending Payouts PDF")]
        public async Task<IActionResult> DownloadInstituteOverviewPdf([FromQuery] Guid? tutorId, [FromQuery] Guid? classId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var instituteId = await GetInstituteIdAsync(userId);
            if (instituteId == Guid.Empty) return NotFound(new { message = "Institute profile not found." });

            var pdfBytes = await _withdrawalService.GenerateInstitutePendingEarningsPdfAsync(instituteId, tutorId, classId);

            if (pdfBytes == null || pdfBytes.Length == 0)
                return NotFound(new { message = "No pending payouts found for the given selection." });

            return File(pdfBytes, "application/pdf", $"Pending_Payouts_Report.pdf");
        }
    }
}
