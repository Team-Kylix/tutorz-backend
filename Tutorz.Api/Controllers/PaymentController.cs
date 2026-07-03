using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tutorz.Application.DTOs.Payment;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IInstituteService _instituteService;
        private readonly IStudentBillService _studentBillService;

        public PaymentController(IPaymentService paymentService, IInstituteService instituteService, IStudentBillService studentBillService)
        {
            _paymentService = paymentService;
            _instituteService = instituteService;
            _studentBillService = studentBillService;
        }

        /// <summary>
        /// GET /api/payment/status?classId=&studentId=
        /// Returns the 15-month payment status strip for a student+class pair.
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetPaymentStatus(
            [FromQuery] Guid classId,
            [FromQuery] Guid studentId)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty)
                return Unauthorized("Institute ID not found in token.");

            var result = await _paymentService.GetPaymentStatusAsync(classId, studentId, instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// POST /api/payment/record
        /// Records a class fee payment for a student.
        /// </summary>
        [HttpPost("record")]
        public async Task<IActionResult> RecordPayment([FromBody] RecordPaymentRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty)
                return Unauthorized("Institute ID not found in token.");

            var result = await _paymentService.RecordPaymentAsync(request, instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/payment/class/{classId}/history?searchQuery=
        /// Returns the history of payments for a particular class (with optional student search), newest first.
        /// </summary>
        [HttpGet("class/history")]
        public async Task<IActionResult> GetClassPaymentHistory(
            [FromQuery] Guid? tutorId,
            [FromQuery] Guid? classId,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty)
                return Unauthorized("Institute ID not found in token.");

            var result = await _paymentService.GetClassPaymentHistoryAsync(instituteId, tutorId, classId, searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        /// <summary>
        /// GET /api/payment/{paymentId}/pdf
        /// Downloads a class payment invoice PDF for the calling institute.
        /// </summary>
        [HttpGet("{paymentId}/pdf")]
        [Authorize(Roles = "Institute,Admin,SuperAdmin")]
        public async Task<IActionResult> DownloadPaymentPdf(Guid paymentId)
        {
            byte[]? pdfBytes = null;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            if (role == "Admin" || role == "SuperAdmin")
            {
                pdfBytes = await _studentBillService.GenerateClassPaymentPdfForSystemAsync(paymentId);
            }
            else
            {
                var instituteId = GetInstituteIdFromToken();
                if (instituteId == Guid.Empty)
                    return Unauthorized("Institute ID not found in token.");

                pdfBytes = await _studentBillService.GenerateClassPaymentPdfForInstituteAsync(paymentId, instituteId);
            }

            if (pdfBytes == null)
                return NotFound("Payment not found or access denied.");

            var reference = $"ClassFee_{paymentId.ToString()[..8].ToUpper()}";
            return File(pdfBytes, "application/pdf", $"Tutorz_{reference}.pdf");
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private Guid GetInstituteIdFromToken()
        {
            var idString = User.FindFirst("InstituteId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
        }
    }
}
