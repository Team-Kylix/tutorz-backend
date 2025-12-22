using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tutorz.Application.DTOs.Tutor;
using Tutorz.Application.Interfaces;

namespace Tutorz.Api.Controllers
{
    [Authorize(Roles = "Tutor")]
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly ITutorService _tutorService;

        public TutorController(ITutorService tutorService)
        {
            _tutorService = tutorService;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

        [HttpPost("classes")]
        public async Task<IActionResult> CreateClass(CreateClassRequest request)
        {
            var result = await _tutorService.CreateClassAsync(GetUserId(), request);
            return Ok(result);
        }

        [HttpPut("classes/{id}")]
        public async Task<IActionResult> UpdateClass(Guid id, CreateClassRequest request)
        {
            var result = await _tutorService.UpdateClassAsync(id, GetUserId(), request);
            return Ok(result);
        }

        [HttpGet("classes")]
        public async Task<IActionResult> GetClasses()
        {
            var result = await _tutorService.GetClassesAsync(GetUserId());
            return Ok(result);
        }

        [HttpPost("classes/add-student")]
        public async Task<IActionResult> AddStudent(AddStudentRequest request)
        {
            await _tutorService.AddStudentToClassAsync(GetUserId(), request);
            return Ok(new { message = "Student added successfully" });
        }

        [HttpDelete("classes/{id}")]
        public async Task<IActionResult> DeleteClass(Guid id)
        {
            await _tutorService.DeleteClassAsync(id, GetUserId());
            return Ok(new { message = "Class deleted successfully" });
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var profile = await _tutorService.GetTutorProfileAsync(userId);
            if (profile == null) return NotFound("Profile not found");
            return Ok(profile);
        }
    }
}