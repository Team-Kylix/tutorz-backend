using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Tutor;
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

        [AllowAnonymous]
        [HttpGet("halls/{instituteId}")]
        public async Task<IActionResult> GetHallsByInstitute(Guid instituteId)
        {
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

        [HttpPost("tutors/request")]
        public async Task<IActionResult> RequestTutorJoin([FromBody] AssignTutorDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SendTutorRequestAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("requests/incoming")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetIncomingRequestsAsync(instituteId);
            return Ok(result);
        }

        [HttpPost("requests/{requestId}/process")]
        public async Task<IActionResult> ProcessRequest(Guid requestId, [FromBody] ProcessJoinRequestDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.ProcessJoinRequestAsync(instituteId, requestId, dto.Action);
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

        [HttpPost("classes")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.CreateInstituteClassAsync(instituteId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("classes/{id}")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] CreateClassRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.UpdateInstituteClassAsync(instituteId, id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("classes/{id}")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.DeleteInstituteClassAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPatch("classes/{id}/status")]
        public async Task<IActionResult> ToggleClassStatus(Guid id)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.ToggleInstituteClassStatusAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetInstituteClassesAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

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

        // --- ATTENDANCE ---

        [HttpGet("attendance/search-student")]
        public async Task<IActionResult> SearchStudentForAttendance([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            // We can reuse GetAssignedStudentsAsync since it already searches among assigned students
            var result = await _instituteService.GetAssignedStudentsAsync(instituteId, query, 1, 50); // Fetch top 50 
            return Ok(result);
        }

        [HttpGet("attendance/student-classes/{studentId}")]
        public async Task<IActionResult> GetStudentClassesForAttendance(Guid studentId)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetStudentClassesForAttendanceAsync(instituteId, studentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("attendance/mark")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.MarkAttendanceAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("attendance/classes-today")]
        public async Task<IActionResult> GetClassesToday()
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetInstituteClassesTodayAsync(instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("attendance/assign-class")]
        public async Task<IActionResult> InstantEnroll([FromBody] InstantEnrollDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.InstantEnrollStudentAsync(instituteId, dto.StudentId, dto.ClassId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("attendance/class-history/{classId}")]
        public async Task<IActionResult> GetClassAttendanceHistory(Guid classId, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? searchQuery)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetClassAttendanceHistoryAsync(instituteId, classId, year, month, searchQuery);
            if (!result.Success) return BadRequest(result);
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