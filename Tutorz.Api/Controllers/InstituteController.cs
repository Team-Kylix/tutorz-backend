using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Api.Attributes;

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
        [ApiPurpose("Get Institute Profile")]
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
        [ApiPurpose("Update Institute Profile")]
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
        [ApiPurpose("Add Hall")]
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
        [ApiPurpose("Get Halls")]
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
        [ApiPurpose("Get Halls by Institute")]
        public async Task<IActionResult> GetHallsByInstitute(Guid instituteId)
        {
            var result = await _hallService.GetHallsAsync(instituteId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("halls/{id}")]
        [ApiPurpose("Update Hall")]
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
        [ApiPurpose("Delete Hall")]
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
        [ApiPurpose("Toggle Hall Status")]
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
        [ApiPurpose("Assign Student to Institute")]
        public async Task<IActionResult> AssignStudent([FromBody] AssignStudentDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.AssignStudentAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("tutors/request")]
        [ApiPurpose("Request Tutor Join")]
        public async Task<IActionResult> RequestTutorJoin([FromBody] AssignTutorDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SendTutorRequestAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("requests/incoming")]
        [ApiPurpose("Get Incoming Join Requests")]
        public async Task<IActionResult> GetIncomingRequests()
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetIncomingRequestsAsync(instituteId);
            return Ok(result);
        }

        [HttpPost("requests/{requestId}/process")]
        [ApiPurpose("Process Join Request")]
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
        [ApiPurpose("Search Institute Students")]
        public async Task<IActionResult> SearchStudents([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SearchStudentsAsync(instituteId, query);
            return Ok(result);
        }

        [HttpGet("tutors/search")]
        [ApiPurpose("Search Institute Tutors")]
        public async Task<IActionResult> SearchTutors([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.SearchTutorsAsync(instituteId, query);
            return Ok(result);
        }

        // --- GET ASSIGNED ---

        [HttpPost("classes")]
        [ApiPurpose("Create Institute Class")]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.CreateInstituteClassAsync(instituteId, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("classes/{id}")]
        [ApiPurpose("Update Institute Class")]
        public async Task<IActionResult> UpdateClass(Guid id, [FromBody] CreateClassRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.UpdateInstituteClassAsync(instituteId, id, request);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpDelete("classes/{id}")]
        [ApiPurpose("Delete Institute Class")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.DeleteInstituteClassAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPatch("classes/{id}/status")]
        [ApiPurpose("Toggle Institute Class Status")]
        public async Task<IActionResult> ToggleClassStatus(Guid id)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.ToggleInstituteClassStatusAsync(instituteId, id);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("classes")]
        [ApiPurpose("Get Institute Classes")]
        public async Task<IActionResult> GetClasses([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetInstituteClassesAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

        [HttpGet("students")]
        [ApiPurpose("Get Assigned Students")]
        public async Task<IActionResult> GetAssignedStudents([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetAssignedStudentsAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

        [HttpGet("tutors")]
        [ApiPurpose("Get Assigned Tutors")]
        public async Task<IActionResult> GetAssignedTutors([FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetAssignedTutorsAsync(instituteId, searchQuery, page, pageSize);
            return Ok(result);
        }

        // --- ATTENDANCE ---

        [HttpGet("attendance/search-student")]
        [ApiPurpose("Search Student for Attendance")]
        public async Task<IActionResult> SearchStudentForAttendance([FromQuery] string query)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            // We can reuse GetAssignedStudentsAsync since it already searches among assigned students
            var result = await _instituteService.GetAssignedStudentsAsync(instituteId, query, 1, 50); // Fetch top 50 
            return Ok(result);
        }

        [HttpGet("attendance/student-classes/{studentId}")]
        [ApiPurpose("Get Student Classes for Attendance")]
        public async Task<IActionResult> GetStudentClassesForAttendance(Guid studentId)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetStudentClassesForAttendanceAsync(instituteId, studentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("attendance/mark")]
        [ApiPurpose("Mark Attendance")]
        public async Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.MarkAttendanceAsync(instituteId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("attendance/classes-today")]
        [ApiPurpose("Get Classes Today")]
        public async Task<IActionResult> GetClassesToday()
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetInstituteClassesTodayAsync(instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("attendance/assign-class")]
        [ApiPurpose("Instant Enroll Student")]
        public async Task<IActionResult> InstantEnroll([FromBody] InstantEnrollDto dto)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.InstantEnrollStudentAsync(instituteId, dto.StudentId, dto.ClassId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("attendance/class-history/{classId}")]
        [ApiPurpose("Get Class Attendance History")]
        public async Task<IActionResult> GetClassAttendanceHistory(Guid classId, [FromQuery] int? month, [FromQuery] int? year, [FromQuery] string? searchQuery)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetClassAttendanceHistoryAsync(instituteId, classId, year, month, searchQuery);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("timetable")]
        [ApiPurpose("Get Timetable by Date")]
        public async Task<IActionResult> GetTimetableByDate([FromQuery] DateTime date)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetClassesByDateAsync(instituteId, date);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        // --- REVENUE & SETTINGS ---

        [HttpGet("revenue")]
        [ApiPurpose("Get Revenue Summary")]
        public async Task<IActionResult> GetRevenueSummary()
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.GetRevenueSummaryAsync(instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("settings/commission")]
        [ApiPurpose("Update Commission Percentage")]
        public async Task<IActionResult> UpdateCommission([FromBody] UpdateCommissionRequest request)
        {
            var instituteId = GetInstituteIdFromToken();
            if (instituteId == Guid.Empty) return Unauthorized("Institute ID not found.");

            var result = await _instituteService.UpdateCommissionAsync(instituteId, request.CommissionPercentage);
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
