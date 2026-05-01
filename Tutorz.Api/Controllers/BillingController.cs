using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Billing;
using Tutorz.Application.Interfaces;

using Tutorz.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Tutorz.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class BillingController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillingController(IBillService billService)
        {
            _billService = billService;
        }

        // --- Admin Endpoints ---

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpGet("bills")]
        public async Task<IActionResult> GetAllBills([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var response = await _billService.GetAllBillsAsync(search, page, pageSize);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("generate")]
        public async Task<IActionResult> TriggerGeneration([FromBody] GenerateBillsRequestDto request)
        {
            var response = await _billService.RolloverOverdueBillsAsync(request.Month, request.Year);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPut("bills/{billId}/mark-paid")]
        public async Task<IActionResult> MarkAsPaid(Guid billId)
        {
            var response = await _billService.MarkBillAsPaidAsync(billId);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [Authorize(Roles = "Admin,SuperAdmin")]
        [HttpPost("fix-references")]
        public async Task<IActionResult> FixOldBillReferences()
        {
            var response = await _billService.FixOldBillReferencesAsync();
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // --- User Endpoints ---

        [HttpGet("my-bills")]
        public async Task<IActionResult> GetMyBills([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var response = await _billService.GetMyBillsAsync(userId, page, pageSize);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        // --- Shared Endpoints ---

        [HttpGet("bills/{billId}")]
        public async Task<IActionResult> GetBillDetail(Guid billId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var response = await _billService.GetBillByIdAsync(billId, userId, role);
            return response.Success ? Ok(response) : BadRequest(response);
        }

        [HttpGet("bills/{billId}/pdf")]
        public async Task<IActionResult> DownloadPdf(Guid billId)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var pdfBytes = await _billService.GenerateBillPdfAsync(billId, userId, role);
            if (pdfBytes == null) return NotFound("Bill not found or access denied.");

            var fileName = $"Invoice_{billId.ToString().Substring(0, 8)}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
    }
}
