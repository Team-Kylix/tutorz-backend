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

        public PaymentController(IPaymentService paymentService, IInstituteService instituteService)
        {
            _paymentService = paymentService;
            _instituteService = instituteService;
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

        // ── Helpers ──────────────────────────────────────────────────────
        private Guid GetInstituteIdFromToken()
        {
            var idString = User.FindFirst("InstituteId")?.Value
                        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(idString, out var id) ? id : Guid.Empty;
        }
    }
}
