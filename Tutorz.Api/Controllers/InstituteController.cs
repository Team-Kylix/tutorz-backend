using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Institute")] // Only Institutes can access
    public class InstituteController : ControllerBase
    {
        private readonly IInstituteService _instituteService;
        private readonly IHallService _hallService;

        public InstituteController(IInstituteService instituteService, IHallService hallService)
        {
            _instituteService = instituteService;
            _hallService = hallService;
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            // FIX: Look for "InstituteId" claim first
            var idString = User.FindFirst("InstituteId")?.Value;

            // Fallback to NameIdentifier if needed
            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _instituteService.GetProfileAsync(instituteId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateInstituteProfileDto dto)
        {
            var idString = User.FindFirst("InstituteId")?.Value;

            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _instituteService.UpdateProfileAsync(instituteId, dto);

            return Ok(result);
        }

        [HttpPost("halls")]
        public async Task<IActionResult> AddHall([FromBody] CreateHallDto dto)
        {
            var idString = User.FindFirst("InstituteId")?.Value;

            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _hallService.AddHallAsync(instituteId, dto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("halls")]
        public async Task<IActionResult> GetHalls()
        {
            var idString = User.FindFirst("InstituteId")?.Value;

            if (string.IsNullOrEmpty(idString))
                idString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idString))
                return Unauthorized("Institute ID not found in token.");

            var instituteId = Guid.Parse(idString);

            var result = await _hallService.GetHallsAsync(instituteId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
        [HttpPut("halls/{id}")]
        public async Task<IActionResult> UpdateHall(Guid id, [FromBody] CreateHallDto dto)
        {
            var idString = User.FindFirst("InstituteId")?.Value;
            // Fallback for older tokens (should be rare now)
            if (string.IsNullOrEmpty(idString)) idString = User.FindFirst("InstituteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
             
            if (string.IsNullOrEmpty(idString)) return Unauthorized("Institute ID not found.");
            var instituteId = Guid.Parse(idString);

            var result = await _hallService.UpdateHallAsync(instituteId, id, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("halls/{id}")]
        public async Task<IActionResult> DeleteHall(Guid id)
        {
            var idString = User.FindFirst("InstituteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idString)) return Unauthorized("Institute ID not found.");
            var instituteId = Guid.Parse(idString);

            var result = await _hallService.DeleteHallAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPatch("halls/{id}/status")]
        public async Task<IActionResult> ToggleHallStatus(Guid id)
        {
            var idString = User.FindFirst("InstituteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idString)) return Unauthorized("Institute ID not found.");
            var instituteId = Guid.Parse(idString);

            var result = await _hallService.ToggleHallStatusAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // --- ASSIGNMENTS ---

        [HttpPost("students/assign")]
        public async Task<IActionResult> AssignStudent([FromBody] AssignStudentDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.AssignStudentAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("tutors/assign")]
        public async Task<IActionResult> AssignTutor([FromBody] AssignTutorDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.AssignTutorAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // --- SEARCH ---

        [HttpGet("students/search")]
        public async Task<IActionResult> SearchStudents([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SearchStudentsAsync(instituteId, query);
            return Ok(result);
        }

        [HttpGet("tutors/search")]
        public async Task<IActionResult> SearchTutors([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SearchTutorsAsync(instituteId, query);
            return Ok(result);
        }

        // --- GET ASSIGNED ---

        [HttpGet("students")]
        public async Task<IActionResult> GetAssignedStudents([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetAssignedStudentsAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

        [HttpGet("tutors")]
        public async Task<IActionResult> GetAssignedTutors([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetAssignedTutorsAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

        private Guid GetInstituteIdFromToken()
        {
            var idString = User.FindFirst("InstituteId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(idString)) return Guid.Empty;
            return Guid.Parse(idString);
        }
    }
}