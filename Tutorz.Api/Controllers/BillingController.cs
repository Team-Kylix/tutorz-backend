using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Tutorz.Application.Interfaces;


namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly IBillService _billService;

        public BillingController(IBillService billService)
        {
            _billService = billService;
        }

        [HttpGet("bills")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> GetAllBills([FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var response = await _billService.GetAllBillsAsync(search, page, pageSize);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("my-bills")]
        public async Task<IActionResult> GetMyBills([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var response = await _billService.GetMyBillsAsync(userId, page, pageSize);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("bills/{billId}")]
        public async Task<IActionResult> GetBillById(Guid billId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();
            var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

            var response = await _billService.GetBillByIdAsync(billId, userId, role);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpPut("bills/{billId}/mark-paid")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        public async Task<IActionResult> MarkBillAsPaid(Guid billId)
        {
            var response = await _billService.MarkBillAsPaidAsync(billId);
            if (!response.Success) return BadRequest(response);
            return Ok(response);
        }

        [HttpGet("bills/{billId}/pdf")]
        public async Task<IActionResult> DownloadBillPdf(Guid billId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdStr, out var userId)) return Unauthorized();

            var response = await _billService.GenerateBillPdfAsync(billId, userId);
            if (!response.Success || response.Data == null) return BadRequest(response.Message);

            return File(response.Data, "application/pdf", $"Platform_Bill_{billId.ToString().Substring(0, 6)}.pdf");
        }
    }
}
