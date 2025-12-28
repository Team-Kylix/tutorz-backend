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
            var studentIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(studentIdString)) return Unauthorized();

            var studentId = Guid.Parse(studentIdString);

            var result = await _studentService.RequestJoinClassAsync(studentId, request.ClassId);

            if (!result.Success) return BadRequest(result);
            return Ok(result);
        }
    }
}