using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Common;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Api.Attributes;
using Tutorz.Domain.Entities;

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

        [HttpGet("dashboard-stats")]
        [ApiPurpose("Get Tutor Dashboard Statistics")]
        public async Task<IActionResult> GetDashboardStats()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetDashboardStatsAsync(userId);
            if (!result.Success)
                return BadRequest(new { message = result.Message });
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

        [HttpPost("classes/{id}/remove-students")]
        [ApiPurpose("Remove all students from Tutor Class")]
        public async Task<IActionResult> RemoveAllStudents(Guid id, [FromQuery] int batchSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.RemoveAllStudentsFromClassAsync(id, userId, batchSize);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result.Data); // Return the BatchOperationResponse
        }

        [HttpPost("classes/{id}/reassign")]
        [ApiPurpose("Reassign all students to another Class")]
        public async Task<IActionResult> ReassignAllStudents(Guid id, [FromBody] ReassignClassDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.ReassignAllStudentsAsync(id, dto.NewClassId, userId, dto.BatchSize);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result.Data); // Return the BatchOperationResponse
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

        [HttpGet("students")]
        [ApiPurpose("Get Tutor Students")]
        public async Task<IActionResult> GetTutorStudents([FromQuery] Guid? instituteId, [FromQuery] Guid? classId, [FromQuery] string searchQuery = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetTutorStudentsAsync(userId, instituteId, classId, searchQuery, page, pageSize);
            
            if (!result.Success) return BadRequest(result);
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

        [HttpGet("students/{studentId}/classes")]
        [ApiPurpose("Get Student Classes for Tutor")]
        public async Task<IActionResult> GetStudentClassesForTutor(Guid studentId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetStudentClassesForTutorAsync(userId, studentId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
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

        [HttpGet("students/search-global")]
        [ApiPurpose("Search All Students in System globally for Tutor")]
        public async Task<IActionResult> SearchStudentsGlobal([FromQuery] string query)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.SearchStudentsGlobalAsync(query);
            
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

        [HttpGet("marks")]
        [ApiPurpose("Get Tutor Mark Sheets")]
        public async Task<IActionResult> GetMarkSheets([FromQuery] Guid? classId, [FromQuery] Guid? instituteId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetMarkSheetsAsync(userId, classId, instituteId);
            return Ok(result);
        }

        [HttpGet("marks/{markSheetId}")]
        [ApiPurpose("Get Mark Sheet Details")]
        public async Task<IActionResult> GetMarkSheetById(Guid markSheetId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.GetMarkSheetByIdAsync(userId, markSheetId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("marks")]
        [ApiPurpose("Create Mark Sheet")]
        public async Task<IActionResult> CreateMarkSheet(Tutorz.Application.DTOs.MarkSheet.CreateMarkSheetDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.CreateMarkSheetAsync(userId, request);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPut("marks/{markSheetId}")]
        [ApiPurpose("Update Mark Sheet")]
        public async Task<IActionResult> UpdateMarkSheet(Guid markSheetId, Tutorz.Application.DTOs.MarkSheet.UpdateMarkSheetDto request)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.UpdateMarkSheetAsync(userId, markSheetId, request);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpDelete("marks/{markSheetId}")]
        [ApiPurpose("Soft Delete Mark Sheet")]
        public async Task<IActionResult> DeleteMarkSheet(Guid markSheetId)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.DeleteMarkSheetAsync(userId, markSheetId);
            if (!result.Success) return BadRequest(new { message = result.Message });
            return Ok(result);
        }

        [HttpPost("attendance/mark")]
        [ApiPurpose("Mark Attendance for Tutor's Student")]
        public async Task<IActionResult> MarkAttendance([FromBody] Tutorz.Application.DTOs.Institute.MarkAttendanceDto dto)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _tutorService.MarkAttendanceAsync(userId, dto);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPost("payments/record")]
        [ApiPurpose("Record Class Payment for Tutor's Student")]
        public async Task<IActionResult> RecordPayment(
            [FromBody] Tutorz.Application.DTOs.Payment.RecordPaymentRequest request,
            [FromServices] IGenericRepository<Class> classRepo)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var cls = await classRepo.GetAsync(c => c.ClassId == request.ClassId);
            if (cls == null) return BadRequest("Class not found.");

            Guid targetInstituteId = cls.InstituteId ?? Guid.Empty;

            var result = await _paymentService.RecordPaymentAsync(request, targetInstituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("payments/status")]
        [ApiPurpose("Get Tutor Student Payment Status Strip")]
        public async Task<IActionResult> GetPaymentStatus(
            [FromQuery] Guid classId,
            [FromQuery] Guid studentId,
            [FromServices] IGenericRepository<Class> classRepo)
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var cls = await classRepo.GetAsync(c => c.ClassId == classId);
            if (cls == null) return BadRequest("Class not found.");

            Guid targetInstituteId = cls.InstituteId ?? Guid.Empty;

            var result = await _paymentService.GetPaymentStatusAsync(classId, studentId, targetInstituteId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}