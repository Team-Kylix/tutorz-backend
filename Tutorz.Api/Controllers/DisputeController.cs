using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Disputes;
using Tutorz.Application.Interfaces;
using Tutorz.Api.Attributes;

namespace Tutorz.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DisputeController : ControllerBase
    {
        private readonly IDisputeService _disputeService;

        public DisputeController(IDisputeService disputeService)
        {
            _disputeService = disputeService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;
            return Guid.TryParse(idClaim, out var id) ? id : Guid.Empty;
        }

        private bool IsAdmin() => User.IsInRole("Admin") || User.IsInRole("SuperAdmin");
        private bool IsSuperAdmin() => User.IsInRole("SuperAdmin");

        // ─────────────────────────────────────────────────────────────────────
        // POST api/dispute  —  Any authenticated user raises a complaint
        // ─────────────────────────────────────────────────────────────────────
        [HttpPost]
        [ApiPurpose("Submit Complaint")]
        public async Task<IActionResult> CreateDispute([FromForm] CreateDisputeDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.CreateDisputeAsync(userId, dto);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET api/dispute/my  —  Current user's own complaints
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("my")]
        [ApiPurpose("Get My Complaints")]
        public async Task<IActionResult> GetMyDisputes(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.GetMyDisputesAsync(userId, page, pageSize);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET api/dispute/{id}  —  Single dispute (owner or Admin)
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet("{id:int}")]
        [ApiPurpose("Get Dispute By Id")]
        public async Task<IActionResult> GetDispute(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.GetDisputeByIdAsync(id, userId, IsAdmin());
            if (!result.Success) return NotFound(new { message = result.Message });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────────────────
        // GET api/dispute  —  Admin/SuperAdmin: scoped disputes with optional search
        //   Regular Admin  → sees Pending + own assigned disputes
        //   SuperAdmin     → sees all disputes
        // ─────────────────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ApiPurpose("Get All Disputes (Admin)")]
        public async Task<IActionResult> GetAllDisputes(
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.GetAllDisputesAsync(
                searchQuery, page, pageSize, userId, IsSuperAdmin());

            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────────────────
        // PATCH api/dispute/{id}/status  —  Admin/SuperAdmin: update dispute status
        //   Auto-assigns the dispute to the calling admin on first status change.
        //   SuperAdmin can update any dispute regardless of assignment.
        // ─────────────────────────────────────────────────────────────────────
        [HttpPatch("{id:int}/status")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ApiPurpose("Update Dispute Status (Admin)")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateDisputeStatusDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.UpdateDisputeStatusAsync(
                id, dto, userId, IsSuperAdmin());

            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE api/dispute/{id}  —  Delete a pending dispute
        // ─────────────────────────────────────────────────────────────────────
        [HttpDelete("{id:int}")]
        [ApiPurpose("Delete Pending Complaint")]
        public async Task<IActionResult> DeleteDispute(int id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _disputeService.DeleteDisputeAsync(id, userId);
            if (!result.Success) return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
    }
}
