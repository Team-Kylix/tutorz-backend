using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Student;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentService _studentService;

        public StudentController(IStudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet("search-classes")]
        public async Task<IActionResult> SearchClasses([FromQuery] string? grade, [FromQuery] string? query)
        {
            // StudentService.SearchClassesAsync already handles null/empty checks using string.IsNullOrEmpty().
            var result = await _studentService.SearchClassesAsync(grade, query);

            if (!result.Success) return BadRequest(result);

            return Ok(result.Data);
        }

        [HttpPost("join-class")]
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

        [HttpGet("profile")]
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
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileDto dto)
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
    }
}