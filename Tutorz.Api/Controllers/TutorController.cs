using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Api.Attributes;

namespace Tutorz.Api.Controllers
{
    [Authorize(Roles = "Tutor")]
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly ITutorService _tutorService;
        private readonly IPaymentService _paymentService;
        private readonly ITutorRepository _tutorRepo;
        private readonly IStudentBillService _studentBillService;

        public TutorController(ITutorService tutorService, IPaymentService paymentService, ITutorRepository tutorRepo, IStudentBillService studentBillService)
        {
            _tutorService = tutorService;
            _paymentService = paymentService;
            _tutorRepo = tutorRepo;
            _studentBillService = studentBillService;
        }

        private Guid GetUserId()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            return Guid.TryParse(idClaim, out var userId) ? userId : Guid.Empty;
        }

        [HttpPost("classes")]
        [ApiPurpose("Create Tutor Class")]
        public async Task<IActionResult> CreateClass(CreateClassRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.CreateClassAsync(userId, request);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPut("classes/{id}")]
        [ApiPurpose("Update Tutor Class")]
        public async Task<IActionResult> UpdateClass(Guid id, CreateClassRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.UpdateClassAsync(id, userId, request);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpGet("classes")]
        [ApiPurpose("Get Tutor Classes")]
        public async Task<IActionResult> GetClasses()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetClassesAsync(userId);
            return Ok(result);
        }

        [HttpPost("classes/add-student")]
        [ApiPurpose("Add Student to Class")]
        public async Task<IActionResult> AddStudent(AddStudentRequest request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            try
            {
                await _tutorService.AddStudentToClassAsync(userId, request);
                return Ok(new { message = "Student added successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("classes/{id}")]
        [ApiPurpose("Delete Tutor Class")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            await _tutorService.DeleteClassAsync(id, userId);
            return Ok(new { message = "Class deleted successfully" });
        }

        [HttpGet("profile")]
        [ApiPurpose("Get Tutor Profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetTutorProfileAsync(userId);
            if (!result.Success) return NotFound(result.Message);
            return Ok(result);
        }

        [HttpPut("profile")]
        [ApiPurpose("Update Tutor Profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateTutorProfileDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.UpdateTutorProfileAsync(userId, request);

            if (!result.Success) return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpGet("requests")]
        [ApiPurpose("Get Student Requests")]
        public async Task<IActionResult> GetRequests()
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty) return Unauthorized();

                var requests = await _tutorService.GetStudentRequestsAsync(userId);
                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("requests/process")]
        [ApiPurpose("Process Student Requests")]
        public async Task<IActionResult> ProcessRequests([FromBody] ProcessRequestDto request)
        {
            try
            {
                var result = await _tutorService.ProcessStudentRequestsAsync(request);

                if (!result)
                    return BadRequest(new { message = "Failed to process requests. No valid pending requests found or invalid action." });

                return Ok(new { message = $"Requests {request.Action.ToLower()} successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("student-profile/{studentId}")]
        [ApiPurpose("Get Student Profile for Tutor")]
        public async Task<IActionResult> GetStudentProfile(Guid studentId)
        {
            try
            {
                var profile = await _tutorService.GetStudentProfileAsync(studentId);

                if (profile == null)
                    return NotFound(new { message = "Student profile not found." });

                return Ok(profile);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("institutes/{instituteId}/request")]
        [ApiPurpose("Request Join Institute")]
        public async Task<IActionResult> RequestJoinInstitute(Guid instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.SendInstituteRequestAsync(userId, instituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("requests/institutes")]
        [ApiPurpose("Get Institute Requests")]
        public async Task<IActionResult> GetInstituteRequests()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetInstituteRequestsAsync(userId);
            return Ok(result);
        }

        [HttpPost("requests/institutes/{requestId}/process")]
        [ApiPurpose("Process Institute Request")]
        public async Task<IActionResult> ProcessInstituteRequest(Guid requestId, [FromBody] ProcessJoinRequestDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.ProcessInstituteRequestAsync(userId, requestId, dto.Action);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("institutes")]
        [ApiPurpose("Get Joined Institutes")]
        public async Task<IActionResult> GetJoinedInstitutes()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetJoinedInstitutesAsync(userId);
            if (!result.Success) return BadRequest(result);
            
            return Ok(result); 
        }

        [HttpGet("students/search")]
        [ApiPurpose("Search Enrolled Students for Tutor")]
        public async Task<IActionResult> SearchStudents([FromQuery] string query)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.SearchStudentsAsync(userId, query);
            
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("institutes/search-exact")]
        [ApiPurpose("Search Institute Exact")]
        public async Task<IActionResult> SearchInstitutesExact([FromQuery] string query)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.SearchInstituteExactAsync(userId, query);

            if (!result.Success) return BadRequest(result);
            return Ok(result.Data); // Return single profile instead of list
        }

        [HttpGet("attendance/history")]
        [ApiPurpose("Get Tutor Attendance History")]
        public async Task<IActionResult> GetAttendanceHistory(
            [FromQuery] Guid? classId,
            [FromQuery] Guid? instituteId,
            [FromQuery] bool noInstitute = false,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetAttendanceHistoryAsync(userId, classId, instituteId, noInstitute, searchQuery, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("payments/history")]
        [ApiPurpose("Get Tutor Payment History")]
        public async Task<IActionResult> GetPaymentHistory(
            [FromQuery] Guid? instituteId,
            [FromQuery] bool noInstitute = false,
            [FromQuery] Guid? classId = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            // Resolve the tutor entity to get the internal TutorId
            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return NotFound(new { message = "Tutor profile not found." });

            var result = await _paymentService.GetTutorPaymentHistoryAsync(
                tutor.TutorId, instituteId, noInstitute, classId, searchQuery, page, pageSize);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("payments/{paymentId}/pdf")]
        [ApiPurpose("Download Tutor Class Payment PDF")]
        public async Task<IActionResult> DownloadPaymentPdf(Guid paymentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var tutor = await _tutorRepo.GetAsync(t => t.UserId == userId);
            if (tutor == null) return NotFound(new { message = "Tutor profile not found." });

            var pdfBytes = await _studentBillService.GenerateClassPaymentPdfForTutorAsync(paymentId, tutor.TutorId);

            if (pdfBytes == null)
                return NotFound("Payment not found or access denied.");

            var reference = $"ClassFee_{paymentId.ToString()[..8].ToUpper()}";
            return File(pdfBytes, "application/pdf", $"Tutorz_{reference}.pdf");
        }
    }
}