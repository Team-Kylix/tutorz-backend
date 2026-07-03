using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.Interfaces;
using Tutorz.Application.DTOs.Institute;
using Tutorz.Api.Attributes;
using System;

namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IStudentBillService _studentBillService;

        public StudentController(IStudentService studentService, IStudentBillService studentBillService)
        {
            _studentService = studentService;
            _studentBillService = studentBillService;
        }

        [HttpGet("search-classes")]
        [ApiPurpose("Search Classes")]
        public async Task<IActionResult> SearchClasses(
            [FromQuery] string? grade, 
            [FromQuery] string? query, 
            [FromQuery] int? provinceId, 
            [FromQuery] int? districtId, 
            [FromQuery] int? cityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            Guid? studentId = null;
            if (!string.IsNullOrEmpty(studentIdString))
            {
                studentId = Guid.Parse(studentIdString);
            }

            var result = await _studentService.SearchClassesAsync(grade, query, studentId, provinceId, districtId, cityId, page, pageSize);

            if (!result.Success) return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpPost("join-class")]
        [ApiPurpose("Student Join Class")]
        public async Task<IActionResult> JoinClass([FromBody] JoinClassRequest request)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);
            var result = await _studentService.RequestJoinClassAsync(studentId, request.ClassId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("leave-class/{classId}")]
        [ApiPurpose("Student Leave Class")]
        public async Task<IActionResult> LeaveClass(Guid classId)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);
            var result = await _studentService.LeaveClassAsync(studentId, classId);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("payment-history")]
        public async Task<IActionResult> GetStudentPaymentHistory(
            [FromQuery] Guid? tutorId,
            [FromQuery] Guid? classId,
            [FromQuery] string? monthYear,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.GetStudentPaymentHistoryAsync(studentId, tutorId, classId, monthYear, page, pageSize);
            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("profile")]
        [ApiPurpose("Get Student Profile")]
        public async Task<IActionResult> GetProfile()
        {
            // Read the specific "StudentId" claim from your token
            var studentIdString = User.FindFirst("StudentId")?.Value;

            // If "StudentId" is missing, try "sub" (NameIdentifier) just in case
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            // Call the service
            var result = await _studentService.GetProfileAsync(studentId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpPut("profile")]
        [ApiPurpose("Update Student Profile")]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateStudentProfileDto dto)
        {
            // Read the specific "StudentId" claim here too
            var studentIdString = User.FindFirst("StudentId")?.Value;

            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.UpdateProfileAsync(studentId, dto);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("classes")]
        [ApiPurpose("Get Joined Classes")]
        public async Task<IActionResult> GetClasses()
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;

            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.GetJoinedClassesAsync(studentId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("timetable")]
        [ApiPurpose("Get Student Timetable by Date")]
        public async Task<IActionResult> GetTimetableByDate([FromQuery] DateTime date)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;

            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.GetClassesByDateAsync(studentId, date);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("attendance-history")]
        [ApiPurpose("Get Student Attendance History")]
        public async Task<IActionResult> GetAttendanceHistory([FromQuery] Guid? tutorId, [FromQuery] Guid? classId, [FromQuery] DateTime? date)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.GetAttendanceHistoryAsync(studentId, tutorId, classId, date);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }

        [HttpGet("tutors/search")]
        [ApiPurpose("Search Tutors for Student")]
        public async Task<IActionResult> SearchTutors([FromQuery] string query)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
            {
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);
            var result = await _studentService.SearchTutorsAsync(studentId, query);
            
            if (!result.Success) return BadRequest(result);
            return Ok(result.Data);
        }

        [HttpGet("class-payments/{paymentId}/pdf")]
        [ApiPurpose("Download Class Payment PDF")]
        public async Task<IActionResult> DownloadClassPaymentPdf(Guid paymentId)
        {
            var studentIdString = User.FindFirst("StudentId")?.Value;
            if (string.IsNullOrEmpty(studentIdString))
                studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(studentIdString))
                return Unauthorized("Student ID not found in token.");

            var studentId = Guid.Parse(studentIdString);
            var pdfBytes = await _studentBillService.GenerateClassPaymentPdfAsync(paymentId, studentId);

            if (pdfBytes == null)
                return NotFound("Payment not found or access denied.");

            var reference = $"ClassFee_{paymentId.ToString()[..8].ToUpper()}";
            return File(pdfBytes, "application/pdf", $"Tutorz_{reference}.pdf");
        }

        [HttpGet("marks")]
        [ApiPurpose("Get Student Marks")]
        public async Task<IActionResult> GetStudentMarks()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _studentService.GetStudentMarksAsync(userId);
            return Ok(result);
        }

        [HttpGet("medals/count")]
        [ApiPurpose("Get Student Medals Count")]
        public async Task<IActionResult> GetStudentMedalsCount()
        {
            var userId = GetUserId();
            if (userId == Guid.Empty) return Unauthorized();

            var result = await _studentService.GetStudentMedalsCountAsync(userId);
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
            return Guid.Empty;
        }
    }
}